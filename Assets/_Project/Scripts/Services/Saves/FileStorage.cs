using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts.Services.Saves
{
    public sealed class FileStorage : IDataStorage
    {
        private const string UserDataDirectoryName = "UserData";
        private readonly string _dirPath;

        public FileStorage()
        {
            _dirPath = Path.Combine(Application.persistentDataPath, UserDataDirectoryName);
            EnsureDirectoryExists();
        }

        public async UniTask<T> LoadState<T>() where T : class, new()
        {
            var path = GetFilePath<T>();
            if (!File.Exists(path))
                return new T();

            try
            {
                var json = await ReadAllTextAsync(path);
                if (string.IsNullOrWhiteSpace(json))
                    return new T();

                var obj = JsonUtility.FromJson<T>(json);
                return obj ?? new T();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FileStorage] Load error for {typeof(T).Name}: {ex}");
                return new T();
            }
        }

        public async UniTask SaveState<T>(T state) where T : class
        {
            EnsureDirectoryExists();

            var path = GetFilePath<T>();
            var tmpPath = path + ".tmp";

            try
            {
                var json = JsonUtility.ToJson(state, prettyPrint: true);
                await WriteAllTextAsync(tmpPath, json);

                if (File.Exists(path))
                    File.Delete(path);
                File.Move(tmpPath, path);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FileStorage] Save error for {typeof(T).Name}: {ex}");
                TryDeleteTemp(tmpPath);
            }
        }

        private void EnsureDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(_dirPath))
                    Directory.CreateDirectory(_dirPath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FileStorage] Can't ensure data directory: {_dirPath}. {ex}");
            }
        }

        private static async UniTask<string> ReadAllTextAsync(string path)
        {
            return await File.ReadAllTextAsync(path).AsUniTask();
        }

        private static async UniTask WriteAllTextAsync(string path, string content)
        {
            await File.WriteAllTextAsync(path, content).AsUniTask();
        }

        private static void TryDeleteTemp(string tmp)
        {
            if (File.Exists(tmp))
                File.Delete(tmp);
        }

        private string GetFilePath<T>() => Path.Combine(_dirPath, $"{typeof(T).Name}.json");
    }
}