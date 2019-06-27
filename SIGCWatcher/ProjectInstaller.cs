using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace CSharp_FileSystemWatcher
{
    /// <summary>
    /// This class is intended to install the Service into the Windows Services pool by using the command installutil
    /// </summary>
    [RunInstaller(true)]
    public class ProjectInstaller : Installer
    {
        #region Private members
        /// <summary>
        /// Defines the name of the Windows Service
        /// </summary>
        private const string WindowsServiceName = "SIGCWatcher";

        /// <summary>
        /// Defines the description of the Windows Service
        /// </summary>
        private const string WindowsServiceDescription = "Servicio para monitorear y sincronizar archivos entre SIGC y Siesa. ";
        
        private ServiceProcessInstaller serviceProcessInstaller;
        private ServiceInstaller serviceInstaller;

        #endregion

        #region Constructors

        public ProjectInstaller()
        {
            serviceProcessInstaller = new ServiceProcessInstaller();
            serviceInstaller = new ServiceInstaller();
            // Here you can set properties on serviceProcessInstaller
            //or register event handlers            
            serviceProcessInstaller.Account = ServiceAccount.LocalService;
            serviceInstaller.ServiceName = WindowsServiceName;
            serviceInstaller.Description = WindowsServiceDescription;            
            this.Installers.AddRange(new Installer[] { serviceProcessInstaller, serviceInstaller });
        }

        #endregion
    }
}