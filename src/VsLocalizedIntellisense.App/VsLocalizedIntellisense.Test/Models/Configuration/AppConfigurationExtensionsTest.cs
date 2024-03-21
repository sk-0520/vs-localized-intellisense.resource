using System;
using System.Configuration;
using System.IO;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VsLocalizedIntellisense.Models.Configuration;
using VsLocalizedIntellisense.Models.Logger;

namespace VsLocalizedIntellisense.Test.Models.Configuration
{
    [TestClass]
    public class AppConfigurationExtensionsTest
    {
        #region variable

        private System.Configuration.Configuration BaseConfiguration { get; set; }

        #endregion

        #region function

        private AppConfiguration Create(string xml, AppConfigurationInitializeParameters initializeParameters)
        {
            if(BaseConfiguration == null) {
                var path = Path.Combine(Test.GetProjectDirectory().FullName, "Models", "Configuration", "AppConfigurationExtensionsTest.config");
                var map = new ExeConfigurationFileMap {
                    ExeConfigFilename = path,
                };
                var config = ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.None);
                BaseConfiguration = config;
            }
            BaseConfiguration.AppSettings.Settings.Clear();

            var xmlDocument = new System.Xml.XmlDocument();
            xmlDocument.LoadXml(xml);

            foreach(XmlElement element in xmlDocument.GetElementsByTagName("add")) {
                var key = element.GetAttribute("key");
                var value = element.GetAttribute("value");

                BaseConfiguration.AppSettings.Settings.Add(new KeyValueConfigurationElement(key, value));
            }

            return new AppConfiguration(BaseConfiguration, initializeParameters);
        }

        [TestMethod]
        public void GetUpdateCheckUriTest()
        {
            var expected = new Uri("http://example.com");
            var xml = $@"
            <appSettings>
                <add key=""update-check-uri"" value=""http://example.com""/>
            </appSettings>";
            var config = Create(xml, new AppConfigurationInitializeParameters(DateTime.UtcNow));
            var actual = config.GetUpdateCheckUri();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GetInstallRootDirectoryPathTest()
        {
            var expected = "%ENV%\\dir";
            var xml = $@"
            <appSettings>
                <add key=""install-root-directory"" value=""%ENV%\dir""/>
            </appSettings>";
            var config = Create(xml, new AppConfigurationInitializeParameters(DateTime.UtcNow));
            var actual = config.GetInstallRootDirectoryPath();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        [DataRow(new string[] { "" }, "")]
        [DataRow(new string[] { "A" }, "A")]
        [DataRow(new string[] { "A", "B" }, "A|B")]
        public void GetIntellisenseDirectoriesTest(string[] expected, string input)
        {
            var xml = $@"
            <appSettings>
                <add key=""intellisense-directories"" value=""{input}""/>
            </appSettings>";
            var config = Create(xml, new AppConfigurationInitializeParameters(DateTime.UtcNow));
            var actual = config.GetIntellisenseDirectories();
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        [DataRow(LogLevel.Information, nameof(LogLevel.Information))]
        [DataRow(LogLevel.Warning, "WARNING")]
        [DataRow(LogLevel.Debug, "debug")]
        [DataRow(LogLevel.Trace, "0")] // 使うべきじゃないけど一応
        public void GetLogDefaultLevelTest(LogLevel expected, string level)
        {
            var xml = $@"
            <appSettings>
                <add key=""log-default-level"" value=""{level}""/>
            </appSettings>";
            var config = Create(xml, new AppConfigurationInitializeParameters(DateTime.UtcNow));
            var actual = config.GetLogDefaultLevel();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        [DataRow(false, "false")]
        [DataRow(false, "False")]
        [DataRow(true, "true")]
        [DataRow(true, "True")]
        public void IsEnableDebugLog_normal_Test(bool expected, string value)
        {
            var xml = $@"
            <appSettings>
                <add key=""log-debug-is-enabled"" value=""{value}""/>
            </appSettings>";
            var config = Create(xml, new AppConfigurationInitializeParameters(DateTime.UtcNow));
            var actual = config.IsEnableDebugLog();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void IsEnableDebugLog_none_Test()
        {
            var xml = $@"
            <appSettings>
            </appSettings>";
            var config = Create(xml, new AppConfigurationInitializeParameters(DateTime.UtcNow));
            var actual = config.IsEnableDebugLog();
            Assert.AreEqual(false, actual);
        }


        #endregion
    }
}