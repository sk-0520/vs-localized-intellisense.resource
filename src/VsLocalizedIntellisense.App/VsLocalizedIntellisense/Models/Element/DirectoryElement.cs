using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VsLocalizedIntellisense.Models.Data;
using VsLocalizedIntellisense.Models.Logger;
using VsLocalizedIntellisense.Models.Service.Application;

namespace VsLocalizedIntellisense.Models.Element
{
    public class DirectoryElement: ElementBase
    {
        #region variable

        private double _downloadPercent = 0;

        #endregion

        public DirectoryElement(DirectoryInfo directory, IEnumerable<LibraryVersionElement> libraryVersionItems, LibraryVersionElement libraryVersion, IEnumerable<IntellisenseVersionElement> intellisenseVersions, IntellisenseVersionElement intellisenseVersion, IEnumerable<LanguageElement> languageItems, LanguageElement language, ILoggerFactory loggerFactory)
            : base(loggerFactory)
        {
            Directory = directory;
            LibraryVersionItems = new ObservableCollection<LibraryVersionElement>(libraryVersionItems);
            LibraryVersion = libraryVersion;

            IntellisenseVersionItems = new ObservableCollection<IntellisenseVersionElement>(intellisenseVersions);
            IntellisenseVersion = intellisenseVersion;

            LanguageItems = new ObservableCollection<LanguageElement>(languageItems);
            Language = language;
        }

        #region property

        public DirectoryInfo Directory { get; }

        /// <summary>
        /// ダウンロード対象か。
        /// </summary>
        public bool IsDownloadTarget { get; set; } = true;

        public ObservableCollection<LibraryVersionElement> LibraryVersionItems { get; set; }
        public LibraryVersionElement LibraryVersion { get; set; }

        public ObservableCollection<IntellisenseVersionElement> IntellisenseVersionItems { get; }
        public IntellisenseVersionElement IntellisenseVersion { get; set; }

        public ObservableCollection<LanguageElement> LanguageItems { get; }
        public LanguageElement Language { get; set; }

        public double DownloadPercent
        {
            get => this._downloadPercent;
            set => SetVariable(ref this._downloadPercent, value);
        }

        #endregion

        #region function

        private async Task<FileInfo> DownloadIntellisenseFileAsync(IntellisenseLanguageParts languageParts, string fileName, DirectoryInfo downloadDirectory, AppFileService fileService, AppIntellisensePageService appIntellisensePageService, CancellationToken cancellationToken)
        {
            var physicalPath = Path.Combine(downloadDirectory.FullName, fileName);
            var repositoryPath = string.Join("/", "intellisense", languageParts.IntellisenseVersion, languageParts.LibraryName, languageParts.Language, fileName);

            using(var contentStream = await appIntellisensePageService.GetDataStreamAsync(repositoryPath, cancellationToken)) {
                using(var physicalStream = new FileStream(physicalPath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read)) {
                    await contentStream.CopyToAsync(physicalStream, 1024 * 4, cancellationToken);
                }
            }

            return new FileInfo(physicalPath);
        }

        private async Task<IList<FileInfo>> DownloadIntellisenseFilesCoreAsync(DirectoryInfo downloadDirectory, AppFileService fileService, AppIntellisensePageService appIntellisensePageService, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var languageParts = new IntellisenseLanguageParts(IntellisenseVersion.DirectoryName, Directory.Name, Language.Language);
            var languageData = fileService.GetIntellisenseLanguageData(languageParts);
            if(languageData != null) {
                Logger.LogInformation($"キャッシュからインテリセンス言語データ取得: {languageParts}");
            } else {
                Logger.LogInformation($"GitHubからインテリセンス言語データ取得: {languageParts}");
                var languageItems = await appIntellisensePageService.GetDataListAsync($"intellisense/{languageParts.IntellisenseVersion}/{languageParts.LibraryName}/{languageParts.Language}", cancellationToken);

                languageData = new IntellisenseLanguageData();
                languageData.LanguageItems = languageItems.Files;
                fileService.SaveIntellisenseLanguageData(languageParts, languageData);
            }

            var result = new List<FileInfo>(languageData.LanguageItems.Length);
            progress.Report(0);
            var percentProgress = new PercentProgress(languageData.LanguageItems.Length, progress);
            foreach(var languageItem in languageData.LanguageItems) {
                var file = await DownloadIntellisenseFileAsync(languageParts, languageItem, downloadDirectory, fileService, appIntellisensePageService, cancellationToken);
                result.Add(file);
                percentProgress.Increment();
            }

            return result;
        }

        public async Task<IList<FileInfo>> DownloadIntellisenseFilesAsync(DirectoryInfo downloadDirectory, AppFileService fileService, AppIntellisensePageService appIntellisensePageService, CancellationToken cancellationToken = default)
        {
            var downloadProgress = new Progress<double>(p => DownloadPercent = p);
            return await DownloadIntellisenseFilesCoreAsync(downloadDirectory, fileService, appIntellisensePageService, downloadProgress, cancellationToken);
        }

        #endregion

        #region ElementBase

        #endregion
    }
}
