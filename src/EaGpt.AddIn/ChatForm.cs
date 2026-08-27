using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EaGpt;

namespace EaGpt.AddIn
{
    internal class ChatForm : Form
    {
        private readonly Func<ComObj?> _repository;
        private readonly EaGptSettings _settings;

        private readonly TextBox _urlBox = new TextBox();
        private readonly ComboBox _modelBox = new ComboBox();
        private readonly Button _refreshButton = new Button();
        private readonly Button _testButton = new Button();
        private readonly RichTextBox _responseBox = new RichTextBox();
        private readonly TextBox _promptBox = new TextBox();
        private readonly Button _askButton = new Button();
        private readonly Button _stopButton = new Button();
        private readonly Button _clearButton = new Button();
        private readonly ComboBox _starterBox = new ComboBox();
        private readonly TextBox _debugBox = new TextBox();
        private readonly TabControl _tabs = new TabControl();
        private readonly List<ChatTurn> _history = new List<ChatTurn>();

        private CancellationTokenSource? _cts;

        public ChatForm(Func<ComObj?> repository)
        {
            _repository = repository;
            _settings = EaGptSettings.Load();

            Text = "EaGPT";
            Width = 720;
            Height = 640;
            MinimumSize = new Size(480, 400);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9F);

            var top = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 36,
                WrapContents = false,
                Padding = new Padding(6, 4, 6, 0)
            };
            top.Controls.Add(new Label { Text = "LLM", AutoSize = true, Padding = new Padding(0, 8, 0, 0) });
            _urlBox.Width = 220;
            _urlBox.Text = _settings.OllamaBaseUrl;
            var tip = new ToolTip();
            tip.SetToolTip(_urlBox,
                "LLM API URL. Ollama: http://localhost:11434. LM Studio: http://localhost:1234. Another machine: http://192.168.1.10:11434 or just 192.168.1.10. That host must listen on the network (OLLAMA_HOST=0.0.0.0).");
            top.Controls.Add(_urlBox);
            top.Controls.Add(new Label { Text = "Model", AutoSize = true, Padding = new Padding(8, 8, 0, 0) });
            _modelBox.Width = 160;
            _modelBox.DropDownStyle = ComboBoxStyle.DropDown;
            _modelBox.Text = _settings.Model;
            top.Controls.Add(_modelBox);
            _refreshButton.Text = "Refresh list";
            _refreshButton.AutoSize = true;
            _refreshButton.Click += (_, __) => RefreshModels();
            top.Controls.Add(_refreshButton);
            _testButton.Text = "Test";
            _testButton.AutoSize = true;
            _testButton.Click += (_, __) => TestConnection();
            top.Controls.Add(_testButton);

            _tabs.Dock = DockStyle.Fill;
            var chatPage = new TabPage("Chat");
            var debugPage = new TabPage("Debug");

            _responseBox.Dock = DockStyle.Fill;
            _responseBox.ReadOnly = true;
            _responseBox.BackColor = Color.White;
            _responseBox.Font = new Font("Consolas", 9.75F);

            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 138 };
            _starterBox.Dock = DockStyle.Top;
            _starterBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _starterBox.Items.Add("(example prompts)");
            _starterBox.Items.Add("Describe the open diagram");
            _starterBox.Items.Add("Audit this model for quality issues");
            _starterBox.Items.Add("What depends on the selected element?");
            _starterBox.Items.Add("Find application components named like the selection");
            _starterBox.Items.Add("Create a business layer view of the current selection");
            _starterBox.Items.Add("Add missing application services for the selected process");
            _starterBox.SelectedIndex = 0;
            _starterBox.SelectedIndexChanged += StarterPicked;

            _promptBox.Multiline = true;
            _promptBox.Dock = DockStyle.Fill;
            _promptBox.AcceptsReturn = true;
            _promptBox.KeyDown += PromptKeyDown;

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                Width = 120,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(6)
            };
            _askButton.Text = "Ask EaGPT";
            _askButton.Width = 100;
            _askButton.Click += (_, __) => Send();
            _stopButton.Text = "Stop";
            _stopButton.Width = 100;
            _stopButton.Enabled = false;
            _stopButton.Click += (_, __) => _cts?.Cancel();
            _clearButton.Text = "Clear chat";
            _clearButton.Width = 100;
            _clearButton.Click += (_, __) => ClearChat();
            buttons.Controls.Add(_askButton);
            buttons.Controls.Add(_stopButton);
            buttons.Controls.Add(_clearButton);
            bottom.Controls.Add(buttons);
            bottom.Controls.Add(_starterBox);
            bottom.Controls.Add(_promptBox);

            chatPage.Controls.Add(_responseBox);
            chatPage.Controls.Add(bottom);

            _debugBox.Dock = DockStyle.Fill;
            _debugBox.Multiline = true;
            _debugBox.ScrollBars = ScrollBars.Both;
            _debugBox.ReadOnly = true;
            _debugBox.Font = new Font("Consolas", 8.25F);
            debugPage.Controls.Add(_debugBox);

            _tabs.TabPages.Add(chatPage);
            _tabs.TabPages.Add(debugPage);

            Controls.Add(_tabs);
            Controls.Add(top);

            FormClosed += (_, __) =>
            {
                PersistSettings();
                _cts?.Cancel();
            };

            Shown += (_, __) =>
            {
                try
                {
                    RefreshModels();
                }
                catch
                {
                    // Ollama may not be running yet
                }
            };
        }

        private void StarterPicked(object? sender, EventArgs e)
        {
            if (_starterBox.SelectedIndex <= 0)
            {
                return;
            }

            string? text = _starterBox.SelectedItem as string;
            _starterBox.SelectedIndex = 0;
            if (!string.IsNullOrWhiteSpace(text))
            {
                _promptBox.Text = text;
                _promptBox.Focus();
                _promptBox.SelectionStart = _promptBox.Text.Length;
            }
        }

        private void ClearChat()
        {
            _history.Clear();
            _responseBox.Clear();
        }

        private void PromptKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.SuppressKeyPress = true;
                Send();
            }
        }

        private OllamaClient CreateClient()
        {
            PersistSettings();
            if (!OllamaEndpoint.TryNormalize(_urlBox.Text, out string normalized, out string error))
            {
                throw new ArgumentException(error);
            }

            _urlBox.Text = normalized;
            return new OllamaClient(normalized, _modelBox.Text.Trim(), _settings.TimeoutMs);
        }

        private void PersistSettings()
        {
            if (OllamaEndpoint.TryNormalize(_urlBox.Text, out string normalized, out _))
            {
                _urlBox.Text = normalized;
                _settings.OllamaBaseUrl = normalized;
            }

            _settings.Model = OllamaClient.SanitizeModelName(_modelBox.Text);
            try
            {
                _settings.Save();
            }
            catch
            {
                // ignore settings IO
            }
        }

        private void TestConnection()
        {
            try
            {
                var client = CreateClient();
                bool ok = client.CheckConnection();
                MessageBox.Show(this,
                    ok ? client.ApiDisplayName + " is reachable at " + client.BaseUrl : "Cannot reach " + client.BaseUrl,
                    "EaGPT",
                    MessageBoxButtons.OK,
                    ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "EaGPT", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void RefreshModels()
        {
            try
            {
                var client = CreateClient();
                var names = client.FetchInstalledModelNames();
                string current = _modelBox.Text;
                _modelBox.Items.Clear();
                foreach (string name in names)
                {
                    _modelBox.Items.Add(name);
                }

                if (!string.IsNullOrWhiteSpace(current))
                {
                    _modelBox.Text = current;
                }
                else if (names.Count > 0)
                {
                    _modelBox.Text = names[0];
                }
            }
            catch (Exception ex)
            {
                AppendResponse("Could not list Ollama models: " + ex.Message + Environment.NewLine);
            }
        }

        private void Send()
        {
            string prompt = _promptBox.Text.Trim();
            if (prompt.Length == 0 || _askButton.Enabled == false)
            {
                return;
            }

            ComObj? repo = _repository();
            if (repo == null)
            {
                MessageBox.Show(this, "Open a project in Enterprise Architect first.", "EaGPT", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;
            _askButton.Enabled = false;
            _stopButton.Enabled = true;
            _promptBox.Clear();
            AppendResponse("You: " + prompt + Environment.NewLine + Environment.NewLine);

            string selection;
            string xml;
            ModelSnapshot snapshot;
            try
            {
                selection = EaModelReader.SelectionContext(repo);
                snapshot = EaModelReader.Read(repo);
                xml = ModelDigestBuilder.ToXml(snapshot);
            }
            catch (Exception ex)
            {
                AppendResponse("Failed to read the EA model: " + ex.Message + Environment.NewLine);
                FinishRequest();
                return;
            }

            string knowledge = KnowledgeRetriever.Retrieve(_settings.KnowledgeFolder, prompt, _settings.KnowledgeMaxChars);
            string analysis = ModelAnalysisContext.Build(snapshot, selection, prompt);
            string userMessage = UserMessageBuilder.BuildUserMessage(selection, xml, prompt, knowledge, analysis);
            string systemPrompt = ArchiMateSystemPrompt.GetSystemPrompt();
            var historyCopy = new List<ChatTurn>(_history);
            _debugBox.Text = "Version 1.0.0" + Environment.NewLine +
                             "LLM: " + _urlBox.Text + " model=" + _modelBox.Text + Environment.NewLine +
                             "History turns: " + historyCopy.Count + Environment.NewLine +
                             "Selection:" + Environment.NewLine + selection + Environment.NewLine +
                             "User message (" + userMessage.Length + " chars)" + Environment.NewLine +
                             userMessage;

            OllamaClient client;
            try
            {
                client = CreateClient();
            }
            catch (Exception ex)
            {
                AppendResponse("Invalid Ollama settings: " + ex.Message + Environment.NewLine);
                FinishRequest();
                return;
            }

            var reply = new StringBuilder();
            Task.Run(() =>
            {
                try
                {
                    string text = client.Chat(systemPrompt, userMessage, delta =>
                    {
                        reply.Append(delta);
                        BeginInvoke(new Action(() => AppendResponse(delta)));
                    }, token, historyCopy);

                    if (reply.Length == 0)
                    {
                        reply.Append(text);
                        BeginInvoke(new Action(() => AppendResponse(text)));
                    }

                    BeginInvoke(new Action(() =>
                    {
                        ChatHistory.Remember(_history, prompt, reply.ToString());
                        ApplyIfChanges(repo, snapshot, reply.ToString(), prompt);
                    }));
                }
                catch (OperationCanceledException)
                {
                    BeginInvoke(new Action(() => AppendResponse(Environment.NewLine + "[stopped]" + Environment.NewLine)));
                }
                catch (Exception ex)
                {
                    BeginInvoke(new Action(() => AppendResponse(Environment.NewLine + "Ollama error: " + ex.Message + Environment.NewLine)));
                }
                finally
                {
                    BeginInvoke(new Action(FinishRequest));
                }
            }, token);
        }

        private void ApplyIfChanges(ComObj repo, ModelSnapshot snapshot, string reply, string prompt)
        {
            AppendResponse(Environment.NewLine + Environment.NewLine);
            if (!ArchiMateLlmResultParser.LooksLikeChangesJson(reply))
            {
                return;
            }

            ArchiMateLlmResult parsed = ArchiMateLlmResultParser.Parse(reply);
            if (!string.IsNullOrEmpty(parsed.Error) && !parsed.HasMutations)
            {
                AppendResponse("Model said: " + parsed.Error + Environment.NewLine);
                return;
            }

            var errors = ArchiMateSchemaValidator.Validate(parsed);
            errors.AddRange(MutationPolicy.CheckLimits(parsed));
            errors.AddRange(RelationshipLegality.Validate(parsed, snapshot));
            if (errors.Count > 0)
            {
                AppendResponse("Validation errors:" + Environment.NewLine + string.Join(Environment.NewLine, errors) + Environment.NewLine);
                AuditLog.TryAppend(null, parsed, prompt, applied: false);
                return;
            }

            ComObj? currentDiagram = EaModelReader.CurrentDiagram(repo);
            if (DiagramCreationIntent.TryDropUnwantedDiagram(parsed, prompt, currentDiagram != null))
            {
                AppendResponse("The reply included a new diagram block; it was ignored because a diagram was open and you did not ask for a new view — shapes were added to the open view." + Environment.NewLine);
            }

            DiagramLayout.Prepare(parsed, snapshot);
            AppendResponse(MutationPolicy.PreviewSummary(parsed) + Environment.NewLine);

            if (MutationPolicy.IsDestructive(parsed))
            {
                DialogResult confirm = MessageBox.Show(
                    this,
                    MutationPolicy.DestructiveSummary(parsed),
                    "EaGPT",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);
                if (confirm != DialogResult.Yes)
                {
                    AppendResponse("Destructive changes were not applied." + Environment.NewLine);
                    AuditLog.TryAppend(null, parsed, prompt, applied: false);
                    return;
                }
            }

            try
            {
                ImportReport report = EaArchiMateImporter.Apply(
                    repo,
                    parsed,
                    EaModelReader.TargetPackage(repo),
                    currentDiagram);
                AppendResponse(report.Summarize() + Environment.NewLine);
                AuditLog.TryAppend(null, parsed, prompt, applied: true);
            }
            catch (Exception ex)
            {
                AppendResponse("Could not apply changes in EA: " + ex.Message + Environment.NewLine);
                AuditLog.TryAppend(null, parsed, prompt, applied: false);
            }
        }

        private void AppendResponse(string text)
        {
            _responseBox.AppendText(text);
            _responseBox.SelectionStart = _responseBox.TextLength;
            _responseBox.ScrollToCaret();
        }

        private void FinishRequest()
        {
            _askButton.Enabled = true;
            _stopButton.Enabled = false;
        }
    }
}
