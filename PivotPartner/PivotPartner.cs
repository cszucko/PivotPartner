using Microsoft.Win32;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Windows.Devices.Sensors;
using Windows.Foundation;


namespace PivotPartner
{
    class PivotPartner
    {
        private const int DMDO_UNKNOWN = -1;

        private NotifyIcon notifyIcon;
        private Int32 currentOrientation;
        private Int32 desiredOrientation;

        private Icon iconUnknown;
        private Icon iconRotate;

        private SimpleOrientationSensor orientationSensor = SimpleOrientationSensor.GetDefault();

        public PivotPartner()
        {
            LoadIcons();
            InitTrayIcon();
            this.currentOrientation = GetCurrentDisplayOrientation();
            this.desiredOrientation = GetCurrentDeviceOrientation();
            UpdateTrayIcon();
        }

        public void StartListeningForChanges()
        {
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
            if (orientationSensor != null)
            {
                orientationSensor.OrientationChanged += SimpleOrientationSensor_OrientationChanged;
            }
        }

        public void StopListeningForChanges()
        {
            SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;
            if (orientationSensor != null)
            {
                orientationSensor.OrientationChanged -= SimpleOrientationSensor_OrientationChanged;
            }
        }

        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            this.currentOrientation = GetCurrentDisplayOrientation();
            UpdateTrayIcon();
        }

        private void SimpleOrientationSensor_OrientationChanged(SimpleOrientationSensor sender, SimpleOrientationSensorOrientationChangedEventArgs e)
        {
            this.desiredOrientation = GetCurrentDeviceOrientation();
            UpdateTrayIcon();
        }

        private void LoadIcons()
        {
            var assembly = Assembly.GetExecutingAssembly();
            string[] resourceNames = assembly.GetManifestResourceNames();
            using (Stream stream = assembly.GetManifestResourceStream(resourceNames.Single(str => str.EndsWith("icon-unknown.ico"))))
            {
                this.iconUnknown = new Icon(stream);
            }
            using (Stream stream = assembly.GetManifestResourceStream(resourceNames.Single(str => str.EndsWith("icon-rotate.ico"))))
            {
                this.iconRotate = new Icon(stream);
            }
        }

        private void InitTrayIcon()
        {
            ContextMenu contextMenu = new ContextMenu();

            MenuItem menuItem = new MenuItem
            {
                Text = "E&xit"
            };
            menuItem.Click += MenuItem_Exit_Click;

            contextMenu.MenuItems.Add(menuItem);

            notifyIcon = new NotifyIcon()
            {
                ContextMenu = contextMenu,
                Visible = true
            };

            notifyIcon.Click += TrayIcon_Click;
        }

        private void MenuItem_Exit_Click(object sender, EventArgs e)
        {
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
            Application.Exit();
        }

        private void TrayIcon_Click(object sender, EventArgs e)
        {
            if (((MouseEventArgs)e).Button == MouseButtons.Left)
            {
                UpdateDisplayOrientation();
            }
        }

        private void UpdateTrayIcon()
        {
            Icon icon;
            if ((this.desiredOrientation == DMDO_UNKNOWN) || (this.currentOrientation == DMDO_UNKNOWN))
            {
                icon = this.iconUnknown;
            } else
            {
                icon = iconRotate;
            }

            string hoverText = "Display: ";

            switch (this.currentOrientation)
            {
                case NativeMethods.DMDO_DEFAULT:
                    hoverText += "0°";
                    break;
                case NativeMethods.DMDO_90:
                    hoverText += "90°";
                    break;
                case NativeMethods.DMDO_180:
                    hoverText += "180°";
                    break;
                case NativeMethods.DMDO_270:
                    hoverText += "270°";
                    break;
                default:
                    hoverText += "?";
                    break;
            }

            hoverText += "\nDevice: ";

            switch (this.desiredOrientation)
            {
                case NativeMethods.DMDO_DEFAULT:
                    hoverText += "0°";
                    break;
                case NativeMethods.DMDO_90:
                    hoverText += "90°";
                    break;
                case NativeMethods.DMDO_180:
                    hoverText += "180°";
                    break;
                case NativeMethods.DMDO_270:
                    hoverText += "270°";
                    break;
                default:
                    hoverText += "?";
                    break;
            }

            notifyIcon.Text = hoverText;
            notifyIcon.Icon = icon;
        }

        private Int32 GetCurrentDisplayOrientation()
        {
            NativeMethods.DEVMODE devmode = new NativeMethods.DEVMODE
            {
                dmDeviceName = new String(new char[32]),
                dmFormName = new String(new char[32])
            };
            devmode.dmSize = (short)Marshal.SizeOf(devmode);

            if (0 != NativeMethods.EnumDisplaySettings(null, NativeMethods.ENUM_CURRENT_SETTINGS, ref devmode))
            {
                return devmode.dmDisplayOrientation;
            }

            return DMDO_UNKNOWN;
        }

        private Int32 GetCurrentDeviceOrientation()
        {
            if (orientationSensor != null)
            {
                switch (orientationSensor.GetCurrentOrientation())
                {
                    case SimpleOrientation.NotRotated:
                        return NativeMethods.DMDO_DEFAULT;
                    case SimpleOrientation.Rotated90DegreesCounterclockwise:
                        return NativeMethods.DMDO_270;
                    case SimpleOrientation.Rotated180DegreesCounterclockwise:
                        return NativeMethods.DMDO_180;
                    case SimpleOrientation.Rotated270DegreesCounterclockwise:
                        return NativeMethods.DMDO_90;
                    case SimpleOrientation.Faceup:
                    case SimpleOrientation.Facedown:
                    default:
                        return DMDO_UNKNOWN;
                }
            }
            return DMDO_UNKNOWN;
        }

        private void UpdateDisplayOrientation()
        {
            if ((this.currentOrientation == this.desiredOrientation) || 
                (this.desiredOrientation == DMDO_UNKNOWN) ||
                (this.currentOrientation == DMDO_UNKNOWN))
            {
                // Do not attempt a change if either state is unknown or if we're already in the desired state
                return;
            }

            NativeMethods.DEVMODE devmode = new NativeMethods.DEVMODE
            {
                dmDeviceName = new String(new char[32]),
                dmFormName = new String(new char[32])
            };
            devmode.dmSize = (short)Marshal.SizeOf(devmode);

            if (0 != NativeMethods.EnumDisplaySettings(null, NativeMethods.ENUM_CURRENT_SETTINGS, ref devmode))
            {
                if (((currentOrientation == NativeMethods.DMDO_DEFAULT) && (desiredOrientation != NativeMethods.DMDO_180)) ||
                    ((currentOrientation == NativeMethods.DMDO_180) && (desiredOrientation != NativeMethods.DMDO_DEFAULT)) ||
                    ((currentOrientation == NativeMethods.DMDO_90) && (desiredOrientation != NativeMethods.DMDO_270)) ||
                    ((currentOrientation == NativeMethods.DMDO_270) && (desiredOrientation != NativeMethods.DMDO_90)))
                {
                    // When rotating 90 degrees we need to swap the height/width
                    int temp = devmode.dmPelsHeight;
                    devmode.dmPelsHeight = devmode.dmPelsWidth;
                    devmode.dmPelsWidth = temp;
                }

                devmode.dmDisplayOrientation = this.desiredOrientation;

                if (0 != NativeMethods.ChangeDisplaySettings(ref devmode, 0))
                {
                    MessageBox.Show("Rotating display failed", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }
}
