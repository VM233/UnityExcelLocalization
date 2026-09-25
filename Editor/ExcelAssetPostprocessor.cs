using UnityEditor;

namespace VM233.ExcelLocalization
{
    internal sealed class ExcelAssetPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            ExcelTableAutoSync.OnAssetsImported(importedAssets, deletedAssets, movedAssets,
                movedFromAssetPaths, didDomainReload);
        }
    }
}
