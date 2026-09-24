using System;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace VM233.ExcelLocalization
{
    public sealed class ExcelTableBuildGuard : BuildPlayerProcessor, IPreprocessBuildWithReport
    {
        public override int callbackOrder => -1000;

        public override void PrepareForBuild(BuildPlayerContext buildPlayerContext)
        {
            try
            {
                // Addressables builds its bundles during PrepareForBuild, before player preprocessing.
                ExcelTableSynchronizer.SyncAll();
            }
            catch (Exception exception)
            {
                throw new BuildFailedException("Excel Localization: " + exception.Message);
            }
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            try
            {
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
