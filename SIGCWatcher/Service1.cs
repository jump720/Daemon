using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Configuration;
using System.Xml.Serialization;
using System.Net.Http;

namespace CSharp_FileSystemWatcher
{
    /// <summary>
    /// This class will run as a Windows Service intended to monitor what files have been added to the filesystem and triggering some actions as a response
    /// </summary>
    public partial class Service1 : ServiceBase
    {
        #region Private variables

        /// <summary>
        /// Keeps a list of all the file watcher listeners, predefined on an XML file
        /// </summary>
        private List<CustomFolderSettings> listFolders;

        /// <summary>
        /// Keeps a list of all the file system watchers in execution.
        /// </summary>
        private List<FileSystemWatcher> listFileSystemWatcher;

        /// <summary>
        /// Name of the XML file where resides the specification of folders and extensions to be monitored
        /// </summary>
        private string fileNameXML;

        #endregion

        #region Constructors

        /// <summary>
        /// Default Constructor
        /// </summary>
        public Service1()
        {
            InitializeComponent();
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Event automatically fired when the service is started by Windows
        /// </summary>
        /// <param name="args">array of arguments</param>
        protected override void OnStart(string[] args)
        {
            // Initialize the list of filesystem Watchers based on the XML configuration file
            PopulateListFileSystemWatchers();

            // Start the file system watcher for each of the file specification and folders found on the List<>
            StartFileSystemWatcher();
        }

        /// <summary>
        /// Event automatically fired when the service is stopped by Windows
        /// </summary>
        protected override void OnStop()
        {
            if (listFileSystemWatcher != null)
            {
                foreach (FileSystemWatcher fsw in listFileSystemWatcher)
                {
                    // Stop listening
                    fsw.EnableRaisingEvents = false;

                    // Record a log entry into Windows Event Log
                    CustomLogEvent(string.Format("Stop monitoring files with extension ({0}) in the folder ({1})", fsw.Filter, fsw.Path));

                    // Dispose the Object
                    fsw.Dispose();
                }

                // Cleans the list
                listFileSystemWatcher.Clear();
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Stops the main execution of the Windows Service
        /// </summary>
        private void StopMainExecution()
        {
            if (listFileSystemWatcher != null)
            {
                foreach (FileSystemWatcher fsw in listFileSystemWatcher)
                {
                    // Stop listening
                    fsw.EnableRaisingEvents = false;

                    // Record a log entry into Windows Event Log
                    CustomLogEvent(string.Format("Stop monitoring files with extension ({0}) in the folder ({1})", fsw.Filter, fsw.Path));

                    // Dispose the Object
                    fsw.Dispose();
                }

                // Cleans the list
                listFileSystemWatcher.Clear();
            }
        }

        /// <summary>
        /// Start the file system watcher for each of the file specification and folders found on the List<>
        /// </summary>
        private void StartFileSystemWatcher()
        {
            // Creates a new instance of the list
            this.listFileSystemWatcher = new List<FileSystemWatcher>();

            // Loop the list to process each of the folder specifications found
            foreach (CustomFolderSettings customFolder in listFolders)
            {
                DirectoryInfo dir = new DirectoryInfo(customFolder.FolderPath);

                // Checks whether the SystemWatcher is enabled and also the directory is a valid location
                if (customFolder.FolderEnabled && dir.Exists)
                {
                    // Creates a new instance of FileSystemWatcher
                    FileSystemWatcher fileSWatch = new FileSystemWatcher();

                    // Sets the filter
                    fileSWatch.Filter = customFolder.FolderFilter;

                    // Sets the folder location
                    fileSWatch.Path = customFolder.FolderPath;

                    // Sets the action to be executed
                    StringBuilder actionToExecute = new StringBuilder(customFolder.ExecutableFile);

                    // List of arguments
                    StringBuilder actionArguments = new StringBuilder(customFolder.ExecutableArguments);

                    // List of arguments
                    StringBuilder WebService = new StringBuilder(customFolder.WebService);

                    // Subscribe to notify filters
                    fileSWatch.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;

                    // Associate the event that will be triggered when a new file is added to the monitored folder, using a lambda Expression                    
                    fileSWatch.Created += (senderObj, fileSysArgs) => fileSWatch_trigger(senderObj, fileSysArgs, actionToExecute.ToString(), WebService.ToString());

                    // Associate the event that will be triggered when a new file is added to the monitored folder, using a lambda Expression                    
                    fileSWatch.Changed += (senderObj, fileSysArgs) => fileSWatch_trigger(senderObj, fileSysArgs, actionToExecute.ToString(), WebService.ToString());

                    // Associate the event that will be triggered when a new file is added to the monitored folder, using a lambda Expression                    
                    fileSWatch.Renamed += (senderObj, fileSysArgs) => fileSWatch_trigger(senderObj, fileSysArgs, actionToExecute.ToString(), WebService.ToString());

                    // Begin watching
                    fileSWatch.EnableRaisingEvents = true;

                    // Add the systemWatcher to the list
                    listFileSystemWatcher.Add(fileSWatch);

                    // Record a log entry into Windows Event Log
                    CustomLogEvent(string.Format("Starting to monitor files with extension ({0}) in the folder ({1})", fileSWatch.Filter, fileSWatch.Path));
                }
                else
                {
                    // Record a log entry into Windows Event Log
                    CustomLogEvent(string.Format("File system monitor cannot start because the folder ({0}) does not exist", customFolder.FolderPath));
                }
            }
        }

        /// <summary>
        /// This event is triggered when a file with the specified extension is created on the monitored folder
        /// </summary>
        /// <param name="sender">Object raising the event</param>
        /// <param name="e">List of arguments - FileSystemEventArgs</param>
        /// <param name="action_Exec">The action_ execute.</param>
        /// <param name="action_Args">The action_ arguments.</param>
        void fileSWatch_trigger(object sender, FileSystemEventArgs e, string action_Exec, string WebService)
        {
            // Gets the name of the file recently added
            string fileName = e.FullPath;

            // Adds the file name to the arguments.  The filename will be placed in lieu of {0}
            //string newStr = string.Format(action_Args, fileName);

            // Executes the command from the DOS window
            //ExecuteCommandLineProcess(action_Exec, newStr, fileName);

            // Call WS to sync
            CallWS(fileName, WebService);
        }

        /// <summary>
        /// Record messages and logs into the Windows Event log
        /// </summary>
        /// <param name="message">Message to be recorded into the Windows Event log</param>
        private void CustomLogEvent(string message)
        {
            string eventSource = this.ServiceName;
            DateTime dt = new DateTime();
            dt = System.DateTime.UtcNow;
            message = dt.ToLocalTime() + ": " + message;

            EventLog.WriteEntry(eventSource, message);
        }

        /// <summary>
        /// Reads an XML file and populates a list of <CustomFolderSettings>
        /// </summary>
        private void PopulateListFileSystemWatchers()
        {
            /// Get the XML file name from the APP.Config file
            fileNameXML = ConfigurationManager.AppSettings["XMLFileFolderSettings"];

            // Creates an instance of XMLSerializer
            XmlSerializer deserializer = new XmlSerializer(typeof(List<CustomFolderSettings>));

            TextReader reader = new StreamReader(fileNameXML);
            object obj = deserializer.Deserialize(reader);

            // Close the TextReader object
            reader.Close();

            // Obtains a list of strongly typed CustomFolderSettings from XML Input data
            listFolders = obj as List<CustomFolderSettings>;
        }

        /// <summary>
        /// Executes a set of instructions through the command window
        /// </summary>
        /// <param name="executableFile">Name of the executable file or program</param>
        /// <param name="argumentList">List of arguments</param>
        private void ExecuteCommandLineProcess(string executableFile, string argumentList, string fileName)
        {
            // Use ProcessStartInfo class.
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.FileName = executableFile;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = argumentList;

            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using-statement will close.
                using (Process exeProcess = Process.Start(startInfo))
                {
                    exeProcess.WaitForExit();

                    // Register a log of the successful operation
                    CustomLogEvent(string.Format("Succesful operation --> Executable: {0} --> Arguments: {1}", executableFile, argumentList));
                }
            }
            catch (Exception exc)
            {
                // Register a Log of the Exception
                CustomLogEvent(exc.Message);
            }
        }

        private async void CallWS(string fileName, string WebService)
        {

            using (HttpClient client = new HttpClient())
            {
                // Call asynchronous network methods in a try/catch block to handle exceptions
                try
                {
                    HttpResponseMessage response = await client.GetAsync(WebService);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    // Above three lines can be replaced with new helper method below
                    // string responseBody = await client.GetStringAsync(uri);

                    // Register a log of the successful operation
                    CustomLogEvent(string.Format("Succesful call WS File --> {0}, answer -- >{1}, Ws -- >{2}", fileName, responseBody, WebService));
                }
                catch (Exception exc)
                {
                    // Register a Log of the Exception
                    CustomLogEvent(exc.Message);
                }
            }
            
        }

        #endregion
    }
}
