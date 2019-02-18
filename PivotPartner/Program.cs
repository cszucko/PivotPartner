using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace PivotPartner
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            PivotPartner pivotPartner = new PivotPartner();
            try
            {
                pivotPartner.StartListeningForChanges();
                Application.Run();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Pivot Partner Terminated Unexpectedly", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                pivotPartner.StopListeningForChanges();
            }
        }
    }
}
