using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace EaGpt.AddIn
{
    /// <summary>
    /// Sparx EA COM add-in entry point. Register as EaGpt.AddIn.EaGptAddIn.
    /// </summary>
    [ComVisible(true)]
    [Guid("B7C4A1E2-3F58-4D9A-9C2B-8E1D6A0F4B31")]
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    [ProgId("EaGpt.AddIn.EaGptAddIn")]
    public class EaGptAddIn
    {
        private const string MenuHeader = "-&EaGPT";
        private const string MenuShow = "&Show EaGPT View";
        private const string MenuAbout = "&About EaGPT";

        private object? _rawRepository;
        private ChatForm? _form;

        public string EA_Connect(object repository)
        {
            _rawRepository = repository;
            return "EaGPT";
        }

        public void EA_Disconnect()
        {
            try
            {
                _form?.Close();
            }
            catch
            {
                // ignore
            }

            _form = null;
            _rawRepository = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public object EA_GetMenuItems(object repository, string location, string menuName)
        {
            _rawRepository = repository ?? _rawRepository;
            if (string.IsNullOrEmpty(menuName))
            {
                return MenuHeader;
            }

            if (menuName == MenuHeader)
            {
                return new[] { MenuShow, MenuAbout };
            }

            return "";
        }

        public void EA_GetMenuState(object repository, string location, string menuName, string itemName, ref bool isEnabled, ref bool isChecked)
        {
            isEnabled = true;
            isChecked = false;
        }

        public void EA_MenuClick(object repository, string location, string menuName, string itemName)
        {
            _rawRepository = repository ?? _rawRepository;
            if (itemName == MenuShow)
            {
                ShowView();
            }
            else if (itemName == MenuAbout)
            {
                MessageBox.Show(
                    "EaGPT 1.0.0 — ArchiGPT-style local Ollama chat for Sparx Enterprise Architect.\n\n" +
                    "MIT License. Inspired by ArchiGPT / Archi-LLM-plugin.",
                    "EaGPT",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        public void EA_OnPostInitialized(object repository)
        {
            _rawRepository = repository;
        }

        public void EA_FileOpen(object repository)
        {
            _rawRepository = repository;
        }

        private void ShowView()
        {
            if (_form != null && !_form.IsDisposed)
            {
                _form.Show();
                _form.BringToFront();
                return;
            }

            _form = new ChatForm(() => _rawRepository == null ? null : new ComObj(_rawRepository));
            _form.FormClosed += (_, __) => _form = null;
            _form.Show();
        }
    }
}
