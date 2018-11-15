using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Storage;

namespace ActorStudio
{
    internal class SettingsHelper : INotifyPropertyChanged
    {
        public event EventHandler SettingsChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        private static SettingsHelper instance;

        static SettingsHelper()
        {
            instance = new SettingsHelper();
        }

        public void Initialize()
        {
            LoadRoamingSettings();
            Windows.Storage.ApplicationData.Current.DataChanged += RoamingDataChanged;
        }

        private void RoamingDataChanged(ApplicationData sender, object args)
        {
            LoadRoamingSettings();
            instance.OnSettingsChanged();
        }

        private void OnSettingsChanged()
        {
            instance.SettingsChanged?.Invoke(instance, EventArgs.Empty);
        }

        private void OnSettingChanged(string propertyName, object value)
        {
            ApplicationData.Current.RoamingSettings.Values[propertyName] = value;

            instance.OnSettingsChanged();
            instance.OnPropertyChanged(propertyName);
        }

        private void OnPropertyChanged(string propertyName)
        {
            instance.PropertyChanged?.Invoke(instance, new PropertyChangedEventArgs(propertyName));
        }

        public static SettingsHelper Instance
        {
            get
            {
                return instance;
            }
        }

        private void LoadRoamingSettings()
        {
            object value = ApplicationData.Current.RoamingSettings.Values[nameof(FaceApiKey)];
            if (value != null)
            {
                this.FaceApiKey = value.ToString();
            }

            value = ApplicationData.Current.RoamingSettings.Values[nameof(FaceApiKeyRegion)];
            if (value != null)
            {
                this.FaceApiKeyRegion = value.ToString();
            }

            value = ApplicationData.Current.RoamingSettings.Values[nameof(FaceListId)];
            if (value != null)
            {
                this.FaceListId = value.ToString();
            }

            value = ApplicationData.Current.RoamingSettings.Values[nameof(CameraName)];
            if (value != null)
            {
                this.CameraName = value.ToString();
            }
        }

        public void RestoreAllSettings()
        {
            ApplicationData.Current.RoamingSettings.Values.Clear();
        }

        private string faceApiKey = string.Empty;
        public string FaceApiKey
        {
            get { return this.faceApiKey; }
            set
            {
                this.faceApiKey = value;
                this.OnSettingChanged(nameof(FaceApiKey), value);
            }
        }

        private string faceApiKeyRegion = string.Empty;
        public string FaceApiKeyRegion
        {
            get { return this.faceApiKeyRegion; }
            set
            {
                this.faceApiKeyRegion = value;
                this.OnSettingChanged(nameof(FaceApiKeyRegion), value);
            }
        }

        private string faceListId = string.Empty;
        public string FaceListId
        {
            get { return this.faceListId; }
            set
            {
                this.faceListId = value;
                this.OnSettingChanged(nameof(FaceListId), value);
            }
        }

        private string cameraName = string.Empty;
        public string CameraName
        {
            get { return cameraName; }
            set
            {
                this.cameraName = value;
                this.OnSettingChanged("CameraName", value);
            }
        }

        public string[] AvailableApiRegions
        {
            get
            {
                return new string[]
                {
                    "westus",
                    "westus2",
                    "eastus",
                    "eastus2",
                    "westcentralus",
                    "southcentralus",
                    "westeurope",
                    "northeurope",
                    "southeastasia",
                    "eastasia",
                    "japaneast",
                    "australiaeast",
                    "brazilsouth"
                };
            }
        }

        internal static async Task<IEnumerable<string>> GetAvailableCameraNamesAsync()
        {
            DeviceInformationCollection deviceInfo = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            return deviceInfo.OrderBy(d => d.Name).Select(d => d.Name);
        }
    }
}
