using System;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace VM233.ExcelLocalization
{
    public sealed class ExcelTableBuildGuard : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            try
            {
                ExcelTableSynchronizer.SyncAll();
                foreach (var binding in ExcelTableSynchronizer.FindBindings())
                {
                    ExcelTableSynchronizer.Verify(binding);
                }
            }
            catch (Exception exception)
            {
                throw new BuildFailedException("Excel Localization: " + exception.Message);
            }
        }
    }
}
