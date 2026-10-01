using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NamulRpgConfigTool
{
    public partial class MainWindow : Window
    {
        private string? _currentFilePath = null;
        private bool _isEnglish = false;
        private bool _isInitialized = false;

        public MainWindow()
        {
            InitializeComponent();
            _isInitialized = true;
            CmbLanguage.SelectedIndex = 0;
            ApplyLanguage(false);
        }

        private void CmbLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            _isEnglish = CmbLanguage.SelectedIndex == 1;
            ApplyLanguage(_isEnglish);
        }

        private void ApplyLanguage(bool en)
        {
            if (!_isInitialized) return;

            if (en)
            {
                TxtTitle.Text = "🌱 Namul RPG Unified Config Tool";
                TxtSubtitle.Text = "TShock Economy & Distance Scaling GUI Editor";
                TxtLangLabel.Text = "🌐 Language: ";

                GrpGeneral.Header = "General Economy & EXP Settings";
                LblCurrencyName.Content = "Currency Name:";
                LblStartingBal.Content = "Starting Balance:";
                LblMaxLevel.Content = "Max Level:";
                LblBaseExp.Content = "Base Required EXP:";
                LblExpPerDmg.Content = "Default EXP / Damage:";
                LblMoneyPerDmg.Content = "Default Gold / Damage:";
                LblBossMult.Content = "Boss Multiplier:";

                GrpDistance.Header = "Distance Scaling Monster Settings (From Spawn)";
                ChkDistanceEnabled.Content = "Enable Distance-Based Monster Scaling";
                LblDistanceMode.Content = "Distance Mode:";
                LblSafeZone.Content = "Safe Zone Radius (Tiles):";
                LblStepTiles.Content = "Scaling Step (Tiles):";
                LblHpStep.Content = "HP Increase per Step (+%):";
                LblDmgStep.Content = "Damage Increase per Step (+%):";
                LblMaxHpMult.Content = "Max HP Multiplier (Cap):";
                LblMaxDmgMult.Content = "Max Damage Multiplier (Cap):";
                ChkScaleReward.Content = "Scale EXP & Money Rewards with Distance";

                BtnOpen.Content = "📂 Load Config File";
                BtnSaveAs.Content = "💾 Save As...";
                BtnSave.Content = "✅ Save Config";
                if (string.IsNullOrEmpty(_currentFilePath))
                {
                    TxtFilePathInfo.Text = "Current File: (Default values loaded)";
                }
            }
            else
            {
                TxtTitle.Text = "🌱 나물 RPG 통합 설정 툴";
                TxtSubtitle.Text = "TShock Economy & Distance Scaling GUI Editor";
                TxtLangLabel.Text = "🌐 언어 / Language: ";

                GrpGeneral.Header = "기본 경제 및 경험치 설정";
                LblCurrencyName.Content = "화폐 이름:";
                LblStartingBal.Content = "신규 유저 정착금:";
                LblMaxLevel.Content = "최대 레벨 (만렙):";
                LblBaseExp.Content = "기본 필요 경험치:";
                LblExpPerDmg.Content = "데미지당 기본 EXP:";
                LblMoneyPerDmg.Content = "데미지당 기본 골드:";
                LblBossMult.Content = "보스 몬스터 배율:";

                GrpDistance.Header = "스폰 지점 거리 비례 몬스터 강화 (Distance Scaling)";
                ChkDistanceEnabled.Content = "거리 비례 몬스터 강화 활성화";
                LblDistanceMode.Content = "거리 계산 방식:";
                LblSafeZone.Content = "안전 지대 반경 (타일):";
                LblStepTiles.Content = "강화 스텝 거리 (타일):";
                LblHpStep.Content = "스텝당 체력 증가 (+%):";
                LblDmgStep.Content = "스텝당 공격력 증가 (+%):";
                LblMaxHpMult.Content = "최대 체력 상한선 (배수):";
                LblMaxDmgMult.Content = "최대 공격력 상한선 (배수):";
                ChkScaleReward.Content = "거리로 강해진 만큼 경험치/골드 보상도 비례 증가";

                BtnOpen.Content = "📂 설정 파일 열기 (Load JSON)";
                BtnSaveAs.Content = "💾 다른 이름으로 저장 (Save As)";
                BtnSave.Content = "✅ 설정 저장 (Save Config)";
                if (string.IsNullOrEmpty(_currentFilePath))
                {
                    TxtFilePathInfo.Text = "현재 불러온 경로: (기본값 세팅됨)";
                }
            }
        }

        private void BtnOpen_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                Title = _isEnglish ? "Select Config JSON File" : "설정 JSON 파일 선택"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    string json = File.ReadAllText(dlg.FileName);
                    var obj = JObject.Parse(json);

                    TxtCurrencyName.Text = (string?)obj["CurrencyName"] ?? "골드";
                    TxtStartingBal.Text = (string?)obj["StartingBalance"] ?? "1000";
                    TxtMaxLevel.Text = (string?)obj["MaxLevel"] ?? "100";
                    TxtBaseExp.Text = (string?)obj["BaseExpRequirement"] ?? "100";
                    TxtExpPerDmg.Text = (string?)obj["DefaultExpPerDamage"] ?? "0.02";
                    TxtMoneyPerDmg.Text = (string?)obj["DefaultMoneyPerDamage"] ?? "0.01";
                    TxtBossMult.Text = (string?)obj["BossMultiplier"] ?? "3.0";

                    var dist = obj["DistanceScaling"];
                    if (dist != null)
                    {
                        ChkDistanceEnabled.IsChecked = (bool?)dist["Enabled"] ?? true;
                        string mode = (string?)dist["DistanceCalculationMode"] ?? "Euclidean";
                        CmbDistanceMode.SelectedIndex = mode.ToLower() switch
                        {
                            "horizontalonly" => 1,
                            "taxicab" => 2,
                            _ => 0
                        };
                        TxtSafeZone.Text = (string?)dist["SafeZoneTileRadius"] ?? "200";
                        TxtStepTiles.Text = (string?)dist["TilesPerScalingStep"] ?? "100";
                        TxtHpStep.Text = (string?)dist["HealthIncreasePerStep"] ?? "0.05";
                        TxtDmgStep.Text = (string?)dist["DamageIncreasePerStep"] ?? "0.03";
                        TxtMaxHpMult.Text = (string?)dist["MaxHealthMultiplier"] ?? "10.0";
                        TxtMaxDmgMult.Text = (string?)dist["MaxDamageMultiplier"] ?? "5.0";
                        ChkScaleReward.IsChecked = (bool?)dist["ScaleRewardsWithDistance"] ?? true;
                    }

                    _currentFilePath = dlg.FileName;
                    TxtFilePathInfo.Text = (_isEnglish ? "Current File: " : "현재 파일: ") + _currentFilePath;
                    MessageBox.Show(_isEnglish ? "Config loaded successfully!" : "설정 파일을 성공적으로 불러왔습니다!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show((_isEnglish ? "Failed to parse JSON: " : "설정 파일 로드 실패: ") + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                BtnSaveAs_Click(sender, e);
                return;
            }

            SaveToFile(_currentFilePath);
        }

        private void BtnSaveAs_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                FileName = "config.json",
                Title = _isEnglish ? "Save Config JSON File" : "설정 JSON 파일 저장"
            };

            if (dlg.ShowDialog() == true)
            {
                _currentFilePath = dlg.FileName;
                SaveToFile(_currentFilePath);
            }
        }

        private void SaveToFile(string path)
        {
            try
            {
                var root = new JObject
                {
                    ["CurrencyName"] = TxtCurrencyName.Text.Trim(),
                    ["StartingBalance"] = long.TryParse(TxtStartingBal.Text, out var sb) ? sb : 1000,
                    ["BaseExpRequirement"] = long.TryParse(TxtBaseExp.Text, out var be) ? be : 100,
                    ["ExpRequirementMultiplier"] = 1.25,
                    ["MaxLevel"] = int.TryParse(TxtMaxLevel.Text, out var ml) ? ml : 100,
                    ["EnableRewards"] = true,
                    ["NotifyRewardsInChat"] = false,
                    ["DefaultExpPerDamage"] = double.TryParse(TxtExpPerDmg.Text, out var ed) ? ed : 0.02,
                    ["DefaultMoneyPerDamage"] = double.TryParse(TxtMoneyPerDmg.Text, out var md) ? md : 0.01,
                    ["BossMultiplier"] = double.TryParse(TxtBossMult.Text, out var bm) ? bm : 3.0,
                    ["BlacklistedNpcNetIds"] = new JArray(488)
                };

                string mode = CmbDistanceMode.SelectedIndex switch
                {
                    1 => "HorizontalOnly",
                    2 => "Taxicab",
                    _ => "Euclidean"
                };

                var dist = new JObject
                {
                    ["Enabled"] = ChkDistanceEnabled.IsChecked == true,
                    ["DistanceCalculationMode"] = mode,
                    ["SafeZoneTileRadius"] = double.TryParse(TxtSafeZone.Text, out var sz) ? sz : 200.0,
                    ["TilesPerScalingStep"] = double.TryParse(TxtStepTiles.Text, out var st) ? st : 100.0,
                    ["HealthIncreasePerStep"] = double.TryParse(TxtHpStep.Text, out var hp) ? hp : 0.05,
                    ["DamageIncreasePerStep"] = double.TryParse(TxtDmgStep.Text, out var dm) ? dm : 0.03,
                    ["MaxHealthMultiplier"] = double.TryParse(TxtMaxHpMult.Text, out var mh) ? mh : 10.0,
                    ["MaxDamageMultiplier"] = double.TryParse(TxtMaxDmgMult.Text, out var mm) ? mm : 5.0,
                    ["ScaleRewardsWithDistance"] = ChkScaleReward.IsChecked == true
                };

                root["DistanceScaling"] = dist;

                string json = JsonConvert.SerializeObject(root, Formatting.Indented);
                File.WriteAllText(path, json);

                TxtFilePathInfo.Text = (_isEnglish ? "Saved File: " : "저장 완료: ") + path;
                MessageBox.Show(_isEnglish ? "Config saved successfully!" : "설정이 성공적으로 저장되었습니다!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show((_isEnglish ? "Failed to save config: " : "저장 실패: ") + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
