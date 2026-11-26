using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;

namespace WindowsDesktopEnhance.Setup
{
    public static class SetupLocalization
    {
        [DllImport("kernel32.dll")]
        private static extern ushort GetUserDefaultUILanguage();

        private static string _current = "zh-CN";
        private static readonly Dictionary<string, Dictionary<string, string>> _tables = BuildTables();

        public static string CurrentLanguage { get { return _current; } }

        public static void InitSystemLanguage()
        {
            string name = CultureInfo.CurrentUICulture.Name;
            try
            {
                ushort id = GetUserDefaultUILanguage();
                if (id != 0) name = new CultureInfo(id).Name;
            }
            catch
            {
            }

            if (name.StartsWith("zh-TW", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("zh-HK", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("zh-MO", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("zh-Hant", StringComparison.OrdinalIgnoreCase))
            {
                _current = "zh-TW";
            }
            else if (name.StartsWith("zh", StringComparison.OrdinalIgnoreCase)) _current = "zh-CN";
            else if (name.StartsWith("ja", StringComparison.OrdinalIgnoreCase)) _current = "ja-JP";
            else if (name.StartsWith("ko", StringComparison.OrdinalIgnoreCase)) _current = "ko-KR";
            else _current = "en-US";
        }

        public static void SetLanguage(string code)
        {
            if (!string.IsNullOrEmpty(code) && _tables.ContainsKey(code))
            {
                _current = code;
            }
        }

        public static string T(string key)
        {
            Dictionary<string, string> dict;
            if (_tables.TryGetValue(_current, out dict))
            {
                string val;
                if (dict.TryGetValue(key, out val)) return val;
            }
            Dictionary<string, string> fallback;
            if (_tables.TryGetValue("en-US", out fallback))
            {
                string val;
                if (fallback.TryGetValue(key, out val)) return val;
            }
            return key;
        }

        private static Dictionary<string, Dictionary<string, string>> BuildTables()
        {
            Dictionary<string, Dictionary<string, string>> all = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

            all["zh-CN"] = new Dictionary<string, string>
            {
                {"Title", "Windows Desktop Enhance 安装向导"},
                {"Subtitle", "将 Windows Desktop Enhance 安装到你的电脑，随时从开始菜单或桌面启动。"},
                {"PathLabel", "安装位置"},
                {"Browse", "浏览..."},
                {"OptionsLabel", "安装选项"},
                {"OptDesktop", "在桌面创建快捷方式"},
                {"OptStartup", "开机自动启动（静默启动到系统托盘）"},
                {"OptLaunch", "安装完成后立即启动应用"},
                {"BtnInstall", "立即安装"},
                {"BtnInstalling", "正在安装..."},
                {"BtnFinish", "完成"},
                {"BtnCancel", "取消"},
                {"StatusReady", "准备就绪，点击「立即安装」开始。"},
                {"StatusDone", "安装完成！已写入开始菜单与卸载信息。"},
                {"StatusErr", "安装失败："},
                {"UninstallTitle", "卸载 Windows Desktop Enhance"},
                {"UninstallConfirm", "确定要从电脑中卸载 Windows Desktop Enhance 吗？（桌面图标位置不会被改动）"},
                {"UninstallDone", "已成功卸载 Windows Desktop Enhance。"}
            };

            all["zh-TW"] = new Dictionary<string, string>
            {
                {"Title", "Windows Desktop Enhance 安裝精靈"},
                {"Subtitle", "將 Windows Desktop Enhance 安裝到您的電腦，隨時從開始功能表或桌面啟動。"},
                {"PathLabel", "安裝位置"},
                {"Browse", "瀏覽..."},
                {"OptionsLabel", "安裝選項"},
                {"OptDesktop", "在桌面建立捷徑"},
                {"OptStartup", "開機自動啟動（靜默啟動到系統匣）"},
                {"OptLaunch", "安裝完成後立即啟動應用程式"},
                {"BtnInstall", "立即安裝"},
                {"BtnInstalling", "正在安裝..."},
                {"BtnFinish", "完成"},
                {"BtnCancel", "取消"},
                {"StatusReady", "準備就緒，點擊「立即安裝」開始。"},
                {"StatusDone", "安裝完成！已建立開始功能表捷徑與解除安裝項目。"},
                {"StatusErr", "安裝失敗："},
                {"UninstallTitle", "解除安裝 Windows Desktop Enhance"},
                {"UninstallConfirm", "確定要從電腦中解除安裝 Windows Desktop Enhance 嗎？（不會更動桌面圖示位置）"},
                {"UninstallDone", "已成功解除安裝 Windows Desktop Enhance。"}
            };

            all["en-US"] = new Dictionary<string, string>
            {
                {"Title", "Windows Desktop Enhance Setup"},
                {"Subtitle", "Install Windows Desktop Enhance on your PC for quick access from Start Menu or Desktop."},
                {"PathLabel", "Install Location"},
                {"Browse", "Browse..."},
                {"OptionsLabel", "Options"},
                {"OptDesktop", "Create a desktop shortcut"},
                {"OptStartup", "Start automatically with Windows (minimized to tray)"},
                {"OptLaunch", "Launch Windows Desktop Enhance after installation"},
                {"BtnInstall", "Install"},
                {"BtnInstalling", "Installing..."},
                {"BtnFinish", "Finish"},
                {"BtnCancel", "Cancel"},
                {"StatusReady", "Ready to install. Click Install to continue."},
                {"StatusDone", "Installation complete! Start Menu shortcut & uninstaller registered."},
                {"StatusErr", "Installation failed: "},
                {"UninstallTitle", "Uninstall Windows Desktop Enhance"},
                {"UninstallConfirm", "Are you sure you want to uninstall Windows Desktop Enhance? (Your desktop icons will not be moved.)"},
                {"UninstallDone", "Windows Desktop Enhance has been uninstalled."}
            };

            all["ja-JP"] = new Dictionary<string, string>
            {
                {"Title", "Windows Desktop Enhance セットアップ"},
                {"Subtitle", "Windows Desktop Enhance をインストールし、スタートメニューやデスクトップから起動できるようにします。"},
                {"PathLabel", "インストール先"},
                {"Browse", "参照..."},
                {"OptionsLabel", "オプション"},
                {"OptDesktop", "デスクトップにショートカットを作成する"},
                {"OptStartup", "Windows 起動時に自動実行する（トレイに最小化）"},
                {"OptLaunch", "インストール完了後にアプリを起動する"},
                {"BtnInstall", "インストール"},
                {"BtnInstalling", "インストール中..."},
                {"BtnFinish", "完了"},
                {"BtnCancel", "キャンセル"},
                {"StatusReady", "準備完了。「インストール」をクリックして開始してください。"},
                {"StatusDone", "インストールが完了しました！"},
                {"StatusErr", "インストールに失敗しました："},
                {"UninstallTitle", "Windows Desktop Enhance のアンインストール"},
                {"UninstallConfirm", "Windows Desktop Enhance をアンインストールしてもよろしいですか？"},
                {"UninstallDone", "Windows Desktop Enhance をアンインストールしました。"}
            };

            all["ko-KR"] = new Dictionary<string, string>
            {
                {"Title", "Windows Desktop Enhance 설치 마법사"},
                {"Subtitle", "Windows Desktop Enhance를 PC에 설치하여 시작 메뉴나 바탕화면에서 바로 실행할 수 있습니다."},
                {"PathLabel", "설치 위치"},
                {"Browse", "찾아보기..."},
                {"OptionsLabel", "설치 옵션"},
                {"OptDesktop", "바탕화면에 바로 가기 만들기"},
                {"OptStartup", "Windows 시작 시 자동 실행 (트레이로 최소화)"},
                {"OptLaunch", "설치 완료 후 앱 바로 실행"},
                {"BtnInstall", "설치"},
                {"BtnInstalling", "설치 중..."},
                {"BtnFinish", "완료"},
                {"BtnCancel", "취소"},
                {"StatusReady", "설치 준비가 완료되었습니다. '설치'를 클릭하세요."},
                {"StatusDone", "설치가 완료되었습니다!"},
                {"StatusErr", "설치 실패: "},
                {"UninstallTitle", "Windows Desktop Enhance 제거"},
                {"UninstallConfirm", "Windows Desktop Enhance를 제거하시겠습니까?"},
                {"UninstallDone", "Windows Desktop Enhance가 제거되었습니다."}
            };

            return all;
        }
    }
}
