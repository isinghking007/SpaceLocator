using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceLoctor.Services
{
    public class FolderScanner
    {
        //Method to scan the root folder 
        //public async Task<Dictionary<string, long>> ScanFolderAsync(string rootFolderPath)
        //{
        //    var result = new ConcurrentDictionary<string, long>();

        //    await Task.Run(() =>
        //    {
        //        CalculateFolderSize(rootFolderPath, result);
        //    });

        //    return result.ToDictionary(k => k.Key, v => v.Value);
        //}

        //Method to Scan the immediate child folders of root 

        public async Task<Dictionary<string, long>> ScanTopFolderAsync(string rootFolderPath, int maxthreads, CancellationToken cancellationToken = default)
        {
            var result = new ConcurrentDictionary<string, long>();

            //Get immediate child folders of root 

            var TopLevelChildFolders = GetDirectoriesSafe(rootFolderPath);
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxthreads,
                CancellationToken = cancellationToken
            };

            await Task.Run(() =>
            {
                Parallel.ForEach(TopLevelChildFolders, options, folder =>
                {
                    long size = CalculateFolderSize(folder, result, cancellationToken);
                    result[folder] = size;
                });
            },cancellationToken);
            //  Calculate ROOT folder size (single-threaded, safe)
            long rootsize = 0;
            foreach(var folder in TopLevelChildFolders)
            {
                if (result.TryGetValue(folder, out var size))
                {
                    rootsize += size;
                }
            }
            // Add files directly under root
            foreach (var file in Directory.GetFiles(rootFolderPath))
            {
                try { rootsize += new FileInfo(file).Length; } catch { }
            }

            result[rootFolderPath] = rootsize;

            return result.ToDictionary(k => k.Key, v => v.Value);
        }

        // Calculates size of folder and all its subfolders (ONCE)
        private long CalculateFolderSize(string folderPath, ConcurrentDictionary<string, long> result,CancellationToken cancellationToken)
        {
            long totalSize = 0;
            cancellationToken.ThrowIfCancellationRequested();

            // 1️⃣ Files directly inside this folder
            try
            {
                foreach (var file in Directory.GetFiles(folderPath))
                {
                    try
                    {
                        totalSize += new FileInfo(file).Length;
                    }
                    catch { }
                }
            }
            catch { }

            // 2️⃣ Subfolders
            foreach (var subFolder in GetDirectoriesSafe(folderPath))
            {
                totalSize += CalculateFolderSize(subFolder, result, cancellationToken);
            }

            // 3️⃣ Store this folder’s size ONCE
            result[folderPath] = totalSize;

            return totalSize;
        }

        private List<string> GetDirectoriesSafe(string path)
        {
            try
            {
                return Directory.GetDirectories(path).ToList();
            }
            catch
            {
                return new List<string>();
            }
        }
    }

}

