namespace CSharp_FileSystemWatcher
{
    partial class Service1
    {
        #region Constants

        /// <summary>
        /// Defines the name of the Windows Service
        /// </summary>
        public string WindowsServiceName = "SIGCWatcher";
        //WindowsServiceName = "SIGC - Watcher";

        #endregion

        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            this.ServiceName = "SIGCWatcher";
        }

        #endregion
    }
}
