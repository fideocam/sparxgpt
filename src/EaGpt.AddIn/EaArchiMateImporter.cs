using System;
using System.Collections.Generic;
using System.Text;
using EaGpt;

namespace EaGpt.AddIn
{
    internal sealed class ImportReport
    {
        public int ElementsAdded { get; set; }
        public int RelationshipsAdded { get; set; }
        public int DiagramsCreated { get; set; }
        public int Removed { get; set; }
        public List<string> Notes { get; } = new List<string>();

        public string Summarize()
        {
            var sb = new StringBuilder();
            sb.Append("Applied: ")
                .Append(ElementsAdded).Append(" element(s), ")
                .Append(RelationshipsAdded).Append(" relationship(s), ")
                .Append(DiagramsCreated).Append(" diagram(s), ")
                .Append(Removed).Append(" removal(s).");
            foreach (string note in Notes)
            {
                sb.Append("\n- ").Append(note);
            }

            return sb.ToString();
        }
    }

    internal static class EaArchiMateImporter
    {
        public const string IdTag = "EaGptId";

        public static ImportReport Apply(ComObj repository, ArchiMateLlmResult result, ComObj? targetPackage, ComObj? targetDiagram)
        {
            var report = new ImportReport();
            var idToElement = new Dictionary<string, ComObj>(StringComparer.OrdinalIgnoreCase);
            IndexExisting(repository, idToElement);

            ComObj package = targetPackage ?? EaModelReader.TargetPackage(repository) ??
                             throw new InvalidOperationException("No package is available to create elements.");

            foreach (var spec in result.Elements)
            {
                if (string.Equals(spec.Type, "View", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(spec.Type, "Diagram", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string type = ArchiMateSchemaValidator.NormalizeElementType(spec.Type);
                string name = spec.Name ?? "";
                string id = IdHelper.EnsureArchiMateId(spec.Id);
                if (FindExisting(idToElement, id, type, name) != null)
                {
                    report.Notes.Add("Skipped existing element " + type + " \"" + name + "\"");
                    continue;
                }

                ComObj? created = CreateElement(package, type, name, id);
                if (created == null)
                {
                    report.Notes.Add("Could not create " + type + " \"" + name + "\"");
                    continue;
                }

                idToElement[id] = created;
                if (!string.IsNullOrEmpty(spec.Id))
                {
                    idToElement[spec.Id!.Trim()] = created;
                }

                report.ElementsAdded++;

                if (targetDiagram != null && result.Diagram == null)
                {
                    PlaceOnDiagram(targetDiagram, created, 50, 50 + (report.ElementsAdded - 1) * 80, 120, 55);
                }
            }

            foreach (var spec in result.Relationships)
            {
                string relType = ArchiMateSchemaValidator.NormalizeRelationshipType(spec.Type);
                ComObj? source = Resolve(repository, idToElement, spec.Source);
                ComObj? target = Resolve(repository, idToElement, spec.Target);
                if (source == null || target == null)
                {
                    report.Notes.Add("Could not resolve relationship " + relType + " " + spec.Source + " -> " + spec.Target);
                    continue;
                }

                string id = IdHelper.EnsureArchiMateId(spec.Id);
                if (CreateConnector(source, target, relType, spec.Name ?? "", id) != null)
                {
                    report.RelationshipsAdded++;
                }
            }

            if (result.Diagram != null && !string.IsNullOrWhiteSpace(result.Diagram.Name))
            {
                ComObj? diagram = FindDiagramByName(repository, result.Diagram.Name!);
                if (diagram == null)
                {
                    diagram = CreateDiagram(package, result.Diagram.Name!, result.Diagram.Viewpoint);
                    if (diagram != null)
                    {
                        report.DiagramsCreated++;
                    }
                }

                if (diagram != null)
                {
                    foreach (var node in result.Diagram.Nodes)
                    {
                        ComObj? el = Resolve(repository, idToElement, node.ElementId);
                        if (el != null)
                        {
                            PlaceOnDiagram(diagram, el, node.X, node.Y, node.Width, node.Height);
                        }
                    }

                    try
                    {
                        repository.Call("ReloadDiagram", diagram.Int("DiagramID"));
                    }
                    catch
                    {
                        // optional
                    }
                }
            }

            report.Removed += RemoveFromDiagram(targetDiagram, repository, idToElement, result.RemoveElementFromDiagramIds, elements: true);
            report.Removed += RemoveFromDiagram(targetDiagram, repository, idToElement, result.RemoveRelationshipFromDiagramIds, elements: false);
            report.Removed += RemoveFromModel(repository, idToElement, result.RemoveElementIds, elements: true);
            report.Removed += RemoveFromModel(repository, idToElement, result.RemoveRelationshipIds, elements: false);
            foreach (string name in result.RemoveDiagramNames)
            {
                if (DeleteDiagramByName(repository, name))
                {
                    report.Removed++;
                }
            }

            return report;
        }

        private static void IndexExisting(ComObj repository, Dictionary<string, ComObj> map)
        {
            foreach (var model in repository.Enumerate("Models"))
            {
                IndexPackage(repository, model, map);
            }
        }

        private static void IndexPackage(ComObj repository, ComObj package, Dictionary<string, ComObj> map)
        {
            foreach (var el in package.Enumerate("Elements"))
            {
                string guid = el.Str("ElementGUID");
                if (!string.IsNullOrEmpty(guid))
                {
                    map[IdHelper.FromEaGuid(guid)] = el;
                    map[guid] = el;
                }

                string tagged = ReadTaggedValue(el, IdTag);
                if (!string.IsNullOrEmpty(tagged))
                {
                    map[tagged] = el;
                }
            }

            foreach (var child in package.Enumerate("Packages"))
            {
                IndexPackage(repository, child, map);
            }
        }

        private static ComObj? FindExisting(Dictionary<string, ComObj> map, string id, string type, string name)
        {
            if (map.TryGetValue(id, out ComObj? byId))
            {
                return byId;
            }

            foreach (var kv in map)
            {
                try
                {
                    if (string.Equals(kv.Value.Str("Name"), name, StringComparison.OrdinalIgnoreCase))
                    {
                        return kv.Value;
                    }
                }
                catch
                {
                    // continue
                }
            }

            return null;
        }

        private static ComObj? CreateElement(ComObj package, string archiType, string name, string id)
        {
            string fq = ArchiMateEaTypeMap.ElementFqType(archiType);
            if (string.IsNullOrEmpty(fq))
            {
                return null;
            }
            ComObj? elements = package.Child("Elements");
            if (elements == null)
            {
                return null;
            }

            ComObj? el = null;
            try
            {
                object? created = elements.Call("AddNew", name, fq);
                if (created != null)
                {
                    el = new ComObj(created);
                }
            }
            catch
            {
                el = null;
            }

            if (el == null)
            {
                try
                {
                    object? created = elements.Call("AddNew", name, "Class");
                    if (created != null)
                    {
                        el = new ComObj(created);
                        try
                        {
                            el.Set("StereotypeEx", fq);
                        }
                        catch
                        {
                            el.Set("Stereotype", fq);
                        }
                    }
                }
                catch
                {
                    return null;
                }
            }

            if (el == null)
            {
                return null;
            }

            try
            {
                el.Set("Name", name);
                el.Call("Update");
                WriteTaggedValue(el, IdTag, id);
                elements.Call("Refresh");
            }
            catch
            {
                // element may still be usable
            }

            return el;
        }

        private static ComObj? CreateConnector(ComObj source, ComObj target, string relType, string name, string id)
        {
            string fq = ArchiMateEaTypeMap.RelationshipFqType(relType);
            if (string.IsNullOrEmpty(fq))
            {
                return null;
            }
            ComObj? connectors = source.Child("Connectors");
            if (connectors == null)
            {
                return null;
            }

            ComObj? c = null;
            try
            {
                object? created = connectors.Call("AddNew", name, fq);
                if (created != null)
                {
                    c = new ComObj(created);
                }
            }
            catch
            {
                try
                {
                    object? created = connectors.Call("AddNew", name, "Association");
                    if (created != null)
                    {
                        c = new ComObj(created);
                        try
                        {
                            c.Set("StereotypeEx", fq);
                        }
                        catch
                        {
                            c.Set("Stereotype", fq);
                        }
                    }
                }
                catch
                {
                    return null;
                }
            }

            if (c == null)
            {
                return null;
            }

            try
            {
                c.Set("SupplierID", target.Int("ElementID"));
                c.Set("Name", name);
                c.Call("Update");
                WriteTaggedValue(c, IdTag, id);
                connectors.Call("Refresh");
            }
            catch
            {
                return null;
            }

            return c;
        }

        private static ComObj? CreateDiagram(ComObj package, string name, string? viewpoint)
        {
            ComObj? diagrams = package.Child("Diagrams");
            if (diagrams == null)
            {
                return null;
            }

            string fq = ArchiMateEaTypeMap.DiagramFqType(viewpoint);
            try
            {
                object? created = diagrams.Call("AddNew", name, fq);
                if (created == null)
                {
                    created = diagrams.Call("AddNew", name, "Logical");
                }

                if (created == null)
                {
                    return null;
                }

                var d = new ComObj(created);
                d.Call("Update");
                diagrams.Call("Refresh");
                return d;
            }
            catch
            {
                return null;
            }
        }

        private static void PlaceOnDiagram(ComObj diagram, ComObj element, int x, int y, int width, int height)
        {
            try
            {
                int eid = element.Int("ElementID");
                foreach (var existing in diagram.Enumerate("DiagramObjects"))
                {
                    if (existing.Int("ElementID") == eid)
                    {
                        return;
                    }
                }

                ComObj? objects = diagram.Child("DiagramObjects");
                if (objects == null)
                {
                    return;
                }

                object? created = objects.Call("AddNew", "", "");
                if (created == null)
                {
                    return;
                }

                var dobj = new ComObj(created);
                dobj.Set("ElementID", eid);
                int left = x <= 0 ? 50 : x;
                int top = y <= 0 ? 50 : y;
                int w = width <= 0 ? 120 : width;
                int h = height <= 0 ? 55 : height;
                dobj.Set("left", left);
                dobj.Set("top", top);
                dobj.Set("right", left + w);
                dobj.Set("bottom", top + h);
                dobj.Call("Update");
                objects.Call("Refresh");
            }
            catch
            {
                // placement is best-effort
            }
        }

        private static ComObj? Resolve(ComObj repository, Dictionary<string, ComObj> map, string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            if (map.TryGetValue(id!.Trim(), out ComObj? found))
            {
                return found;
            }

            string archi = IdHelper.EnsureArchiMateId(id);
            if (map.TryGetValue(archi, out found))
            {
                return found;
            }

            try
            {
                string eaGuid = IdHelper.ToEaGuid(id);
                ComObj? byGuid = repository.CallObj("GetElementByGuid", eaGuid);
                if (byGuid != null)
                {
                    return byGuid;
                }
            }
            catch
            {
                // ignore
            }

            return null;
        }

        private static ComObj? FindDiagramByName(ComObj repository, string name)
        {
            foreach (var model in repository.Enumerate("Models"))
            {
                ComObj? d = FindDiagramInPackage(model, name);
                if (d != null)
                {
                    return d;
                }
            }

            return null;
        }

        private static ComObj? FindDiagramInPackage(ComObj package, string name)
        {
            foreach (var d in package.Enumerate("Diagrams"))
            {
                if (string.Equals(d.Str("Name"), name, StringComparison.OrdinalIgnoreCase))
                {
                    return d;
                }
            }

            foreach (var child in package.Enumerate("Packages"))
            {
                ComObj? d = FindDiagramInPackage(child, name);
                if (d != null)
                {
                    return d;
                }
            }

            return null;
        }

        private static bool DeleteDiagramByName(ComObj repository, string name)
        {
            foreach (var model in repository.Enumerate("Models"))
            {
                if (DeleteDiagramInPackage(model, name))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool DeleteDiagramInPackage(ComObj package, string name)
        {
            ComObj? diagrams = package.Child("Diagrams");
            if (diagrams != null)
            {
                int count = diagrams.Int("Count");
                for (int i = count - 1; i >= 0; i--)
                {
                    try
                    {
                        object? item = diagrams.Call("GetAt", (short)i);
                        if (item != null && string.Equals(new ComObj(item).Str("Name"), name, StringComparison.OrdinalIgnoreCase))
                        {
                            return package.TryDeleteAt("Diagrams", i);
                        }
                    }
                    catch
                    {
                        // continue
                    }
                }
            }

            foreach (var child in package.Enumerate("Packages"))
            {
                if (DeleteDiagramInPackage(child, name))
                {
                    return true;
                }
            }

            return false;
        }

        private static int RemoveFromDiagram(ComObj? diagram, ComObj repository, Dictionary<string, ComObj> map, List<string> ids, bool elements)
        {
            if (diagram == null || ids.Count == 0)
            {
                return 0;
            }

            int n = 0;
            foreach (string id in ids)
            {
                if (elements)
                {
                    ComObj? el = Resolve(repository, map, id);
                    if (el == null)
                    {
                        continue;
                    }

                    int eid = el.Int("ElementID");
                    ComObj? objects = diagram.Child("DiagramObjects");
                    if (objects == null)
                    {
                        continue;
                    }

                    int count = objects.Int("Count");
                    for (int i = count - 1; i >= 0; i--)
                    {
                        try
                        {
                            object? item = objects.Call("GetAt", (short)i);
                            if (item != null && new ComObj(item).Int("ElementID") == eid)
                            {
                                if (diagram.TryDeleteAt("DiagramObjects", i))
                                {
                                    n++;
                                }
                            }
                        }
                        catch
                        {
                            // continue
                        }
                    }
                }
            }

            try
            {
                repository.Call("ReloadDiagram", diagram.Int("DiagramID"));
            }
            catch
            {
                // optional
            }

            return n;
        }

        private static int RemoveFromModel(ComObj repository, Dictionary<string, ComObj> map, List<string> ids, bool elements)
        {
            int n = 0;
            foreach (string id in ids)
            {
                ComObj? item = Resolve(repository, map, id);
                if (item == null)
                {
                    continue;
                }

                try
                {
                    if (elements)
                    {
                        int pkgId = item.Int("PackageID");
                        ComObj? pkg = repository.CallObj("GetPackageByID", pkgId);
                        if (pkg != null && DeleteCollectionItem(pkg, "Elements", "ElementID", item.Int("ElementID")))
                        {
                            n++;
                        }
                    }
                    else
                    {
                        int client = item.Int("ClientID");
                        ComObj? src = repository.CallObj("GetElementByID", client);
                        if (src != null && DeleteCollectionItem(src, "Connectors", "ConnectorID", item.Int("ConnectorID")))
                        {
                            n++;
                        }
                    }
                }
                catch
                {
                    // continue
                }
            }

            return n;
        }

        private static bool DeleteCollectionItem(ComObj owner, string collection, string idProperty, int id)
        {
            ComObj? coll = owner.Child(collection);
            if (coll == null)
            {
                return false;
            }

            int count = coll.Int("Count");
            for (int i = count - 1; i >= 0; i--)
            {
                try
                {
                    object? item = coll.Call("GetAt", (short)i);
                    if (item != null && new ComObj(item).Int(idProperty) == id)
                    {
                        return owner.TryDeleteAt(collection, i);
                    }
                }
                catch
                {
                    // continue
                }
            }

            return false;
        }

        private static string ReadTaggedValue(ComObj owner, string name)
        {
            try
            {
                foreach (var tv in owner.Enumerate("TaggedValues"))
                {
                    if (string.Equals(tv.Str("Name"), name, StringComparison.OrdinalIgnoreCase))
                    {
                        return tv.Str("Value");
                    }
                }
            }
            catch
            {
                // no tags
            }

            return "";
        }

        private static void WriteTaggedValue(ComObj owner, string name, string value)
        {
            try
            {
                foreach (var tv in owner.Enumerate("TaggedValues"))
                {
                    if (string.Equals(tv.Str("Name"), name, StringComparison.OrdinalIgnoreCase))
                    {
                        tv.Set("Value", value);
                        tv.Call("Update");
                        return;
                    }
                }

                ComObj? tags = owner.Child("TaggedValues");
                if (tags == null)
                {
                    return;
                }

                object? created = tags.Call("AddNew", name, value);
                if (created != null)
                {
                    new ComObj(created).Call("Update");
                    tags.Call("Refresh");
                }
            }
            catch
            {
                // tagged values are optional
            }
        }
    }
}
