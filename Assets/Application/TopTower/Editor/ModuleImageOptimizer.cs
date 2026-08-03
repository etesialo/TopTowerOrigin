using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace KS.TopTower.EditorTools
{
    /// <summary>
    /// 모듈 이미지 최적화기.
    /// bigger 폴더(원본 마스터)의 png를 비율로 판별해 세로 195(롱)/52(숏), 가로 150 단위로 축소한
    /// 최적화본을 목적지 폴더에 저장한다.
    ///
    /// 크기 규칙:
    ///  - 롱: 세로 195 고정, 가로 = 슬롯수 x 150
    ///  - 숏: 세로 52 고정,  가로 = 슬롯수 x 150
    ///  - 판별: 세로 195 가정 시 가로가 150 정수배면 롱, 세로 52 가정 시 정수배면 숏 (딱 떨어지는 쪽).
    ///
    /// 폴더 라우팅(사용자 지정 전까지):
    ///  - Empty / Consinterior 계열  -> StageCommon (모든 스테이지 공통)
    ///  - 그 외                       -> Stage_{nnn} (파일명 앞 3자리 스테이지 번호)
    ///
    /// 덮어쓰기 정책 A: 최적화본이 없거나 bigger가 더 최신일 때만 생성(기존 최신본 보존).
    /// </summary>
    public static class ModuleImageOptimizer
    {
        private const string ModuleRoot = "Assets/Application/TopTower/Image/Module";
        private const string BiggerFolder = ModuleRoot + "/bigger";
        private const string CommonFolder = ModuleRoot + "/StageCommon";

        private const int SlotWidth = 150;
        private const int LongHeight = 195;
        private const int ShortHeight = 52;
        private const int MaxSlots = 8;          // 슬롯수 상한 (롱/숏 판별 모호성 컷)
        private const float SlotTolerance = 0.08f;

        [MenuItem("Tools/Top Tower/Optimize Module Images (bigger)")]
        public static void OptimizeMenu()
        {
            int created, skipped, failed;
            string log = Optimize(out created, out skipped, out failed);
            Debug.Log("[ModuleImageOptimizer] 완료. 생성 " + created + " / 스킵 " + skipped + " / 실패 " + failed + "\n" + log);
        }

        /// <summary>bigger 스캔 -> 최적화본 생성/갱신 + Sprite 임포트 + Sync. 요약 로그 반환.</summary>
        public static string Optimize(out int created, out int skipped, out int failed)
        {
            created = 0; skipped = 0; failed = 0;
            var log = new StringBuilder();

            if (!Directory.Exists(BiggerFolder))
            {
                log.AppendLine("bigger 폴더 없음: " + BiggerFolder);
                return log.ToString();
            }

            var newFiles = new List<string>();   // 이번에 새로 만든 최적화본(임포트 보정 대상)

            foreach (var raw in Directory.GetFiles(BiggerFolder, "*.png"))
            {
                string srcPath = raw.Replace('\\', '/');
                string file = Path.GetFileName(srcPath);
                string name = Path.GetFileNameWithoutExtension(srcPath);

                string destFolder = ResolveDestFolder(name);
                if (destFolder == null)
                {
                    log.AppendLine("스킵(스테이지 번호 판정 실패): " + file);
                    skipped++;
                    continue;
                }
                string destPath = destFolder + "/" + file;
                bool existed = File.Exists(destPath);

                // 정책 A: 최적화본이 이미 있고 bigger보다 최신(같거나)이면 스킵
                if (existed && File.GetLastWriteTimeUtc(destPath) >= File.GetLastWriteTimeUtc(srcPath))
                {
                    skipped++;
                    continue;
                }

                var srcTex = AssetDatabase.LoadAssetAtPath<Texture2D>(srcPath);
                if (srcTex == null)
                {
                    AssetDatabase.ImportAsset(srcPath, ImportAssetOptions.ForceSynchronousImport);
                    srcTex = AssetDatabase.LoadAssetAtPath<Texture2D>(srcPath);
                }
                if (srcTex == null) { log.AppendLine("실패(로드): " + file); failed++; continue; }

                int tw, th; string cls;
                if (!ResolveTargetSize(srcTex.width, srcTex.height, out tw, out th, out cls))
                {
                    log.AppendLine("실패(슬롯 판정 불가 r=" + ((float)srcTex.width / srcTex.height).ToString("0.000") + "): " + file);
                    failed++;
                    continue;
                }

                if (!Directory.Exists(destFolder)) Directory.CreateDirectory(destFolder);

                if (ResizeAndSave(srcPath, destPath, tw, th))
                {
                    if (!existed) newFiles.Add(destPath);
                    log.AppendLine(file + "  ->  " + cls + " " + tw + "x" + th + "  [" + destFolder.Substring(ModuleRoot.Length + 1) + "]");
                    created++;
                }
                else { log.AppendLine("실패(리사이즈/저장): " + file); failed++; }
            }

            AssetDatabase.Refresh();

            // 새로 만든 최적화본은 Sprite 임포트로 보정(같은 폴더 기존본 설정 복사)
            foreach (var p in newFiles) EnsureSpriteImporter(p);

            TopTowerAddressablesSyncTool.Sync();
            return log.ToString();
        }

        /// <summary>nnn_ 접두어 = 스테이지 전용(Stage_nnn), 없으면 = 모든 스테이지 공통(StageCommon).</summary>
        private static string ResolveDestFolder(string name)
        {
            var m = Regex.Match(name, @"^(\d{3})_");
            if (m.Success) return ModuleRoot + "/Stage_" + m.Groups[1].Value;
            return CommonFolder;
        }

        /// <summary>비율로 롱/숏 + 슬롯수 판정 -> 목표 픽셀 크기.</summary>
        private static bool ResolveTargetSize(int w, int h, out int tw, out int th, out string cls)
        {
            tw = 0; th = 0; cls = null;
            if (h <= 0) return false;
            float r = (float)w / h;

            float longF = (float)LongHeight * r / SlotWidth;
            int longS = Mathf.RoundToInt(longF);
            float longE = Mathf.Abs(longF - longS);

            float shortF = (float)ShortHeight * r / SlotWidth;
            int shortS = Mathf.RoundToInt(shortF);
            float shortE = Mathf.Abs(shortF - shortS);

            bool longOk = longS >= 1 && longS <= MaxSlots && longE <= SlotTolerance;
            bool shortOk = shortS >= 1 && shortS <= MaxSlots && shortE <= SlotTolerance;

            if (longOk && (!shortOk || longE <= shortE)) { th = LongHeight; tw = longS * SlotWidth; cls = "롱"; return true; }
            if (shortOk) { th = ShortHeight; tw = shortS * SlotWidth; cls = "숏"; return true; }
            return false;
        }

        /// <summary>원본을 목표 크기로 area-average 축소 후 PNG 저장. 원본은 임시로 readable/무압축 처리 후 복원.</summary>
        private static bool ResizeAndSave(string srcPath, string destPath, int tw, int th)
        {
            var ti = AssetImporter.GetAtPath(srcPath) as TextureImporter;
            bool restore = false;
            bool prevReadable = false;
            TextureImporterCompression prevComp = TextureImporterCompression.Compressed;
            int prevMax = 2048;

            if (ti != null)
            {
                prevReadable = ti.isReadable;
                prevComp = ti.textureCompression;
                prevMax = ti.maxTextureSize;
                if (!ti.isReadable || ti.textureCompression != TextureImporterCompression.Uncompressed || ti.maxTextureSize < 8192)
                {
                    ti.isReadable = true;
                    ti.textureCompression = TextureImporterCompression.Uncompressed;
                    ti.maxTextureSize = 8192;
                    ti.SaveAndReimport();
                    restore = true;
                }
            }

            try
            {
                var src = AssetDatabase.LoadAssetAtPath<Texture2D>(srcPath);
                if (src == null) return false;

                Color32[] sp = src.GetPixels32();
                Color32[] dp = AreaResample(sp, src.width, src.height, tw, th);

                var dst = new Texture2D(tw, th, TextureFormat.RGBA32, false);
                dst.SetPixels32(dp);
                dst.Apply();
                byte[] png = dst.EncodeToPNG();
                Object.DestroyImmediate(dst);

                File.WriteAllBytes(Path.GetFullPath(destPath), png);
                return true;
            }
            finally
            {
                if (restore && ti != null)
                {
                    ti.isReadable = prevReadable;
                    ti.textureCompression = prevComp;
                    ti.maxTextureSize = prevMax;
                    ti.SaveAndReimport();
                }
            }
        }

        /// <summary>커버리지 가중 박스(area) 축소. 프리멀티플라이 알파로 투명 경계 색 번짐 방지.</summary>
        private static Color32[] AreaResample(Color32[] src, int sw, int sh, int dw, int dh)
        {
            var dst = new Color32[dw * dh];
            float sxScale = (float)sw / dw;
            float syScale = (float)sh / dh;

            for (int dy = 0; dy < dh; dy++)
            {
                float sy0 = dy * syScale, sy1 = (dy + 1) * syScale;
                int iy0 = Mathf.FloorToInt(sy0), iy1 = Mathf.Min(sh - 1, Mathf.CeilToInt(sy1) - 1);

                for (int dx = 0; dx < dw; dx++)
                {
                    float sx0 = dx * sxScale, sx1 = (dx + 1) * sxScale;
                    int ix0 = Mathf.FloorToInt(sx0), ix1 = Mathf.Min(sw - 1, Mathf.CeilToInt(sx1) - 1);

                    double rw = 0, gw = 0, bw = 0;   // 프리멀티플 누적 (알파 가중)
                    double aw = 0;                   // 알파*커버리지 누적
                    double cov = 0;                  // 커버리지 누적

                    for (int sy = iy0; sy <= iy1; sy++)
                    {
                        float wy = Mathf.Min(sy1, sy + 1) - Mathf.Max(sy0, sy);
                        if (wy <= 0f) continue;
                        int row = sy * sw;
                        for (int sx = ix0; sx <= ix1; sx++)
                        {
                            float wx = Mathf.Min(sx1, sx + 1) - Mathf.Max(sx0, sx);
                            if (wx <= 0f) continue;
                            float w = wx * wy;
                            Color32 c = src[row + sx];
                            double a = c.a / 255.0;
                            rw += c.r * w * a;
                            gw += c.g * w * a;
                            bw += c.b * w * a;
                            aw += w * a;
                            cov += w;
                        }
                    }

                    byte outA = (byte)Mathf.Clamp(Mathf.RoundToInt((float)(cov > 0 ? (aw / cov) * 255.0 : 0)), 0, 255);
                    byte outR, outG, outB;
                    if (aw > 0)
                    {
                        outR = (byte)Mathf.Clamp(Mathf.RoundToInt((float)(rw / aw)), 0, 255);
                        outG = (byte)Mathf.Clamp(Mathf.RoundToInt((float)(gw / aw)), 0, 255);
                        outB = (byte)Mathf.Clamp(Mathf.RoundToInt((float)(bw / aw)), 0, 255);
                    }
                    else { outR = outG = outB = 0; }

                    dst[dy * dw + dx] = new Color32(outR, outG, outB, outA);
                }
            }
            return dst;
        }

        /// <summary>새 최적화본을 Sprite로 임포트 설정. 같은 폴더 기존 스프라이트 설정을 템플릿으로 복사.</summary>
        private static void EnsureSpriteImporter(string path)
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) return;

            var tmpl = FindTemplateImporter(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;

            if (tmpl != null)
            {
                ti.spritePixelsPerUnit = tmpl.spritePixelsPerUnit;
                ti.filterMode = tmpl.filterMode;
                ti.mipmapEnabled = tmpl.mipmapEnabled;
                ti.textureCompression = tmpl.textureCompression;
                ti.wrapMode = tmpl.wrapMode;
                ti.maxTextureSize = Mathf.Max(tmpl.maxTextureSize, 1024);
            }
            else
            {
                ti.mipmapEnabled = false;
                ti.filterMode = FilterMode.Bilinear;
            }
            ti.SaveAndReimport();
        }

        /// <summary>같은 폴더의 다른 png를 임포트 설정 템플릿으로.</summary>
        private static TextureImporter FindTemplateImporter(string newPath)
        {
            string folder = Path.GetDirectoryName(newPath).Replace('\\', '/');
            foreach (var f in Directory.GetFiles(folder, "*.png"))
            {
                string p = f.Replace('\\', '/');
                if (p == newPath) continue;
                var ti = AssetImporter.GetAtPath(p) as TextureImporter;
                if (ti != null && ti.textureType == TextureImporterType.Sprite) return ti;
            }
            return null;
        }
    }
}
