using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services;
using System.Diagnostics;
using Newtonsoft.Json;
using System.IO;

namespace SFPkg.StatefulSvc
{
    public class SFPkgStatefulSvc : StatefulService
    {
        protected override ICommunicationListener CreateCommunicationListener()
        {
            // TODO: Replace this with an ICommunicationListener implementation if your service needs to handle user requests.
            return base.CreateCommunicationListener();
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following with your own logic.
            var myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>("myDictionary");

            // Register to listen to config packages changes
            ServiceInitializationParameters.CodePackageActivationContext.ConfigurationPackageModifiedEvent += ConfigPkgChanged;
            ServiceInitializationParameters.CodePackageActivationContext.ConfigurationPackageAddedEvent += ConfigPkgAdded;
            ServiceInitializationParameters.CodePackageActivationContext.ConfigurationPackageRemovedEvent += ConfigPkgRemoved;

            // register to listen to data packages changes
            ServiceInitializationParameters.CodePackageActivationContext.DataPackageAddedEvent += DataPkgAdded;
            ServiceInitializationParameters.CodePackageActivationContext.DataPackageModifiedEvent += DataPkgChanged;
            ServiceInitializationParameters.CodePackageActivationContext.DataPackageRemovedEvent += DataPkgRemoved;

            while (!cancellationToken.IsCancellationRequested)
            {



                WriteConfig();
                WriteData();


                using (var tx = this.StateManager.CreateTransaction())
                {
                    var result = await myDictionary.TryGetValueAsync(tx, "Counter-1");
                    ServiceEventSource.Current.ServiceMessage(
                        this,
                        "Current Counter Value: {0}",
                        result.HasValue ? result.Value.ToString() : "Value does not exist.");

                    await myDictionary.AddOrUpdateAsync(tx, "Counter-1", 0, (k, v) => ++v);

                    await tx.CommitAsync();
                }

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }


        private void WriteConfig()
        {
            // Get Config package
            var configPkg = ServiceInitializationParameters.CodePackageActivationContext.GetConfigurationPackageObject("Config");

            // working with standard configuration file settings.xml
            // settings file has to be XML as it is surfaces in Settings member below (fabric takes care of loading for you
            var customSection = configPkg.Settings.Sections["customSettingsSection"];

            foreach (var p in customSection.Parameters)
            {
                Debug.WriteLine(string.Format("*** Custom Settings Section Param:{0} -> {1}", p.Name, p.Value));
            }

            // working with none standard configuration file other than settings.xml
            // this are treated as blobs by fabric so we can use JSON or any other format of choice
            var customConfigFilePath = configPkg.Path + @"\CustomConfig.json";

            // Config Master is a POCO object where we are keeping the configuration
            ConfigMaster configMaster = JsonConvert.DeserializeObject<ConfigMaster>(File.ReadAllText(customConfigFilePath));
            Debug.WriteLine(string.Format("*** Custom Config File ConfigKey -> {0}", configMaster.ConfigKey));
        }

        private void WriteData()
        {
            // get the data package
            var DataPkg = ServiceInitializationParameters.CodePackageActivationContext.GetDataPackageObject("SvcData");

            // fabric doesn't load data it is just manages for you. data is opaque to Fabric
            var customDataFilePath = DataPkg.Path + @"\StaticDataMaster.json";


            // Config Master is a POCO object where we are keeping the configuration
            StaticDataMaster dataMaster = JsonConvert.DeserializeObject<StaticDataMaster>(File.ReadAllText(customDataFilePath));

            Debug.WriteLine("******** Start Data *********");

            Debug.WriteLine("*** States ***");
            foreach (var state in dataMaster.States)
                Debug.WriteLine(string.Format("State:{0} Code:{1}", state.Name, state.Code));
            

            Debug.WriteLine("*** Zones ***");

            foreach (var zone in dataMaster.Zones)
                Debug.WriteLine(string.Format("State:{0} Code:{1}", zone.Name, zone.Code));
            

            Debug.WriteLine("******** End Data *********");
        }



        /// EVENT HANDLERS
        /// 
        private void DataPkgRemoved(object sender, System.Fabric.PackageRemovedEventArgs<System.Fabric.DataPackage> e)
        {
            Debug.WriteLine("***Data Package Removed!" + e.Package.Path);
            Debugger.Break();
        }

        private void DataPkgChanged(object sender, System.Fabric.PackageModifiedEventArgs<System.Fabric.DataPackage> e)
        {
            Debug.WriteLine("***Data Package Changed!" + e.NewPackage.Path);
            Debugger.Break();
        }

        private void DataPkgAdded(object sender, System.Fabric.PackageAddedEventArgs<System.Fabric.DataPackage> e)
        {
            Debug.WriteLine("***Data Package Added!" + e.Package.Path);
            Debugger.Break();
        }

        private void ConfigPkgRemoved(object sender, System.Fabric.PackageRemovedEventArgs<System.Fabric.ConfigurationPackage> e)
        {
            Debug.WriteLine("***Config Package Removed!" + e.Package.Path);
            Debugger.Break();
        }

        private void ConfigPkgAdded(object sender, System.Fabric.PackageAddedEventArgs<System.Fabric.ConfigurationPackage> e)
        {
            Debug.WriteLine("***Config Package Added!" + e.Package.Path);
            Debugger.Break();
        }

        private void ConfigPkgChanged(object sender, System.Fabric.PackageModifiedEventArgs<System.Fabric.ConfigurationPackage> e)
        {
            Debug.WriteLine("***Config Package Updated!" + e.NewPackage.Path);
            Debugger.Break();
        }
    }
}
