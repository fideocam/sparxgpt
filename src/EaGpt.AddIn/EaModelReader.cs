using System;
using System.Collections.Generic;
using EaGpt;

namespace EaGpt.AddIn
{
    internal static class EaModelReader
    {
        public static ModelSnapshot Read(ComObj repository, int maxElements = 400)
        {
            var snapshot = new ModelSnapshot
            {
                Name = SafeName(repository)
            };
            var seenConnectors = new HashSet<int>();
            int elementCount = 0;

            foreach (var model in repository.Enumerate("Models"))
            {
                WalkPackage(repository, model, snapshot, seenConnectors, ref elementCount, maxElements);
            }

            return snapshot;
        }

        public static string SelectionContext(ComObj repository)
        {
            var lines = new List<string>();
            try
            {
                ComObj? diagram = repository.CallObj("GetCurrentDiagram");
                if (diagram != null)
                {
                    string dName = diagram.Str("Name");
                    lines.Add("Primary diagram (open in editor) \"" + dName + "\"");
                    foreach (var dobj in diagram.Enumerate("SelectedObjects"))
                    {
                        int elementId = dobj.Int("ElementID");
                        ComObj? el = repository.CallObj("GetElementByID", elementId);
                        if (el != null)
                        {
                            lines.Add(DescribeElement(el) + " on diagram \"" + dName + "\"");
                        }
                    }

                    ComObj? selectedConnector = diagram.Child("SelectedConnector");
                    if (selectedConnector != null && selectedConnector.Int("ConnectorID") != 0)
                    {
                        lines.Add(DescribeConnector(selectedConnector, repository) + " on diagram \"" + dName + "\"");
                    }
                }
            }
            catch
            {
                // ignore
            }

            try
            {
                ComObj? pkg = repository.CallObj("GetTreeSelectedPackage");
                if (pkg != null)
                {
                    lines.Add("Package \"" + pkg.Str("Name") + "\"");
                }
            }
            catch
            {
                // ignore
            }

            try
            {
                object? selected = repository.Call("GetTreeSelectedElements");
                if (selected is string csv && csv.Length > 0)
                {
                    foreach (string part in csv.Split(','))
                    {
                        if (int.TryParse(part.Trim(), out int id) && id > 0)
                        {
                            ComObj? el = repository.CallObj("GetElementByID", id);
                            if (el != null)
                            {
                                lines.Add(DescribeElement(el));
                            }
                        }
                    }
                }
            }
            catch
            {
                // ignore
            }

            return SelectionContextFormatter.Format(lines);
        }

        public static ComObj? CurrentDiagram(ComObj repository)
        {
            try
            {
                return repository.CallObj("GetCurrentDiagram");
            }
            catch
            {
                return null;
            }
        }

        public static ComObj? TargetPackage(ComObj repository)
        {
            try
            {
                ComObj? pkg = repository.CallObj("GetTreeSelectedPackage");
                if (pkg != null)
                {
                    return pkg;
                }
            }
            catch
            {
                // ignore
            }

            foreach (var model in repository.Enumerate("Models"))
            {
                return model;
            }

            return null;
        }

        internal static string DescribeElement(ComObj el)
        {
            string type = ArchiMateEaTypeMap.FromEaStereotype(FirstStereotype(el.Str("StereotypeEx"), el.Str("Stereotype")), relationship: false);
            string line = "Element " + type + " \"" + el.Str("Name") + "\" (id=" + IdHelper.FromEaGuid(el.Str("ElementGUID")) + ")";
            string notes = FlattenNotes(el.Str("Notes"));
            if (notes.Length > 0)
            {
                line += " notes: " + notes;
            }

            return line;
        }

        private static string FlattenNotes(string? notes)
        {
            if (string.IsNullOrWhiteSpace(notes))
            {
                return "";
            }

            var sb = new System.Text.StringBuilder(notes!.Length);
            bool space = false;
            foreach (char c in notes)
            {
                if (c == '\r' || c == '\n' || c == '\t')
                {
                    space = true;
                    continue;
                }

                if (c < ' ')
                {
                    continue;
                }

                if (space)
                {
                    sb.Append(' ');
                    space = false;
                }

                sb.Append(c);
                if (sb.Length >= 220)
                {
                    sb.Append('…');
                    break;
                }
            }

            return sb.ToString().Trim();
        }

        internal static string DescribeConnector(ComObj c, ComObj repository)
        {
            string type = ArchiMateEaTypeMap.FromEaStereotype(FirstStereotype(c.Str("StereotypeEx"), c.Str("Stereotype")), relationship: true);
            string src = NameById(repository, c.Int("ClientID"));
            string tgt = NameById(repository, c.Int("SupplierID"));
            return "Relationship " + type + " \"" + c.Str("Name") + "\" (id=" + IdHelper.FromEaGuid(c.Str("ConnectorGUID")) + ") from \"" + src + "\" to \"" + tgt + "\"";
        }

        private static string NameById(ComObj repository, int id)
        {
            try
            {
                ComObj? el = repository.CallObj("GetElementByID", id);
                return el == null ? "?" : el.Str("Name");
            }
            catch
            {
                return "?";
            }
        }

        private static string FirstStereotype(string stereoEx, string stereo)
        {
            if (!string.IsNullOrWhiteSpace(stereoEx))
            {
                int comma = stereoEx.IndexOf(',');
                return comma > 0 ? stereoEx.Substring(0, comma) : stereoEx;
            }

            return stereo;
        }

        private static string SafeName(ComObj repository)
        {
            try
            {
                return repository.Str("ConnectionString");
            }
            catch
            {
                return "EA model";
            }
        }

        private static void WalkPackage(ComObj repository, ComObj package, ModelSnapshot snapshot, HashSet<int> seenConnectors, ref int elementCount, int maxElements)
        {
            if (elementCount >= maxElements)
            {
                return;
            }

            foreach (var el in package.Enumerate("Elements"))
            {
                if (elementCount >= maxElements)
                {
                    break;
                }

                string elType = el.Str("Type");
                if (elType.Equals("Package", StringComparison.OrdinalIgnoreCase) ||
                    elType.Equals("Note", StringComparison.OrdinalIgnoreCase) ||
                    elType.Equals("Text", StringComparison.OrdinalIgnoreCase) ||
                    elType.Equals("Boundary", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string stereo = FirstStereotype(el.Str("StereotypeEx"), el.Str("Stereotype"));
                snapshot.Elements.Add(new SnapshotElement
                {
                    Id = IdHelper.FromEaGuid(el.Str("ElementGUID")),
                    Type = ArchiMateEaTypeMap.FromEaStereotype(stereo, relationship: false),
                    Name = el.Str("Name")
                });
                elementCount++;

                foreach (var c in el.Enumerate("Connectors"))
                {
                    int cid = c.Int("ConnectorID");
                    if (cid == 0 || !seenConnectors.Add(cid))
                    {
                        continue;
                    }

                    string cst = FirstStereotype(c.Str("StereotypeEx"), c.Str("Stereotype"));
                    snapshot.Relationships.Add(new SnapshotRelationship
                    {
                        Id = IdHelper.FromEaGuid(c.Str("ConnectorGUID")),
                        Type = ArchiMateEaTypeMap.FromEaStereotype(cst, relationship: true),
                        Source = GuidByElementId(repository, c.Int("ClientID")),
                        Target = GuidByElementId(repository, c.Int("SupplierID")),
                        Name = c.Str("Name")
                    });
                }
            }

            foreach (var diagram in package.Enumerate("Diagrams"))
            {
                var snap = new SnapshotDiagram
                {
                    Id = IdHelper.FromEaGuid(diagram.Str("DiagramGUID")),
                    Name = diagram.Str("Name"),
                    Viewpoint = diagram.Str("Stereotype")
                };
                foreach (var dobj in diagram.Enumerate("DiagramObjects"))
                {
                    try
                    {
                        int eid = dobj.Int("ElementID");
                        snap.Nodes.Add(new SnapshotNode
                        {
                            ElementId = GuidByElementId(repository, eid),
                            X = dobj.Int("left"),
                            Y = dobj.Int("top"),
                            Width = Math.Max(40, dobj.Int("right") - dobj.Int("left")),
                            Height = Math.Max(20, dobj.Int("bottom") - dobj.Int("top"))
                        });
                    }
                    catch
                    {
                        // skip node
                    }
                }

                snapshot.Diagrams.Add(snap);
            }

            foreach (var child in package.Enumerate("Packages"))
            {
                WalkPackage(repository, child, snapshot, seenConnectors, ref elementCount, maxElements);
            }
        }

        private static string GuidByElementId(ComObj repository, int elementId)
        {
            if (elementId <= 0)
            {
                return "";
            }

            try
            {
                ComObj? el = repository.CallObj("GetElementByID", elementId);
                return el == null ? "" : IdHelper.FromEaGuid(el.Str("ElementGUID"));
            }
            catch
            {
                return "";
            }
        }
    }
}
