# 显像管的外侧 · Outside the Picture Tube

> 用 Unity 搭的一座纸剧场，为あいみょん《生きていたんだよな》做的一支非官方 MV。
> 本仓库公开**制作工程**：搭景与布光脚本、镜头时钟、角色表演数据、合成与字幕工具、设计文档。
> **不含**歌曲音频、歌词原文与译文、成片视频。

*An unofficial music video for Aimyon's "Ikiteitanda yo na", built as a paper theatre in Unity. This repository holds the production
project: set-building and lighting scripts, the shot clock, the singer's hand-keyed performance, compositing and caption tools, and the
design documents. The song, its lyrics and the rendered film are not included.*

> **内容提示**：这首歌和这支片子都涉及自杀。片中直接出现了坠落与红纸代表的血。
> 如果你正在经历困难，请告诉身边的人，或联系当地的心理援助热线。

---

## 这是什么

- 3 分 18 秒，83 个镜头，2560×1440 / 24 fps。
- 一座会自己搭景和撤景的纸剧场：纸楼一片片滑进来，路灯从地板槽里升起，纸云挂在线上降下。
- 一排长着动物头的纸人围观，手里的对话卡会翻亮。
- 一张红纸在追光里自己折成鸟，从唱歌的人掌心飞出台口。
- 唱歌的角色 lin（VRoid，VRM 1.0）全曲逐帧手 K，带立麦、口型、表情和视线。
- 开场房间里的老电视，屏幕里是纸剧场本身。

## 目录

```
unity/                 Unity 6000.3.28f1（URP）工程
  Assets/Stage/        影片时钟与机位（FilmClock、FilmCams、FilmV3）、两个烘焙器（FilmBaker、ShotBaker）、着色器
  Assets/MV/           演唱驱动（SingDriver 等，手 K 数据播放、握麦、口型、视线）、立麦预制体、角色 lin
  Assets/Props/        室内道具（Poly Haven，CC0）、动物头纸面具（AI 生成）
  Assets/Paper/        纸面布景贴图（AI 生成后经脚本处理）
  Assets/Textures/     纸板、纸、墙、地板等材质贴图（ambientCG / Poly Haven，CC0）
  Assets/Gen/          脚本生成的材质、网格、贴图
  Assets/Scenes/       Stage（纸剧场）、Room（房间）
  AgentScripts/src/    搭景、布光、机位检查、出片入口（按文件名顺序拼成 Sets.cs 后运行）
  Data/                lin 的全曲手 K 数据（key_song.bin）、影片版（key_film.bin）、驱动参数
tools/                 Python / shell 工具：镜头表、合成、字幕排版、封面、AI 生成调用、检查图
docs/                  镜头表 film_v5.json、全片时间表、搭景计划、设计图纸、返修记录、字幕模板
gen/                   AI 生成的视频和图片（含提示词）
```

## 环境

- Unity 6000.3.28f1，URP 17.3。包见 `unity/Packages/manifest.json`（UniVRM 0.131.4、uLipSync 3.1.5 从 GitHub 拉取）。
- [Unity CLI](https://unity.com/) 与 `com.unity.pipeline` 包：`tools/sets.sh` 用 `unity command script run` 驱动正在运行的编辑器。
- Python 3.11 以上：numpy、Pillow、pykakasi；ffmpeg。
- 字体（未随仓库分发）：Shippori Mincho B1、Zen Old Mincho、Zen Kaku Gothic New、Dela Gothic One、Klee One（均为 OFL，可从 Google Fonts 下载）；
  中文小字和罗马音用的是 Windows 自带的微软雅黑、Segoe UI 和 Noto Serif SC。

## 你需要自己准备的

| 东西 | 放在哪 | 说明 |
|---|---|---|
| 歌曲音频 | `audio/song.wav` | 193.09 秒。版权属于原作者，本仓库不提供。 |
| 歌词与译文 | `docs/字幕.json` | 格式见 `docs/字幕_待填.json`：每行 `[起, 止, 日文, 中文, 罗马音]`，时间已填好。也可以把“日文 / 罗马音 / 中文”三行一组的文本交给 `tools/captions_build.py` 生成。 |

## 出片流程（简述）

1. 打开 `unity/` 工程和 `Stage` 场景。
2. **电视画面**：`tools/sets.sh SetupFilmBaker '[0,4634,1,1440,1080,"tv"]'`，然后进入播放模式，把整部片从电视机位渲一遍。开场的三段 AI 视频由 `tools/tv_clips.py` 写进电视帧。
3. **舞台镜头**：`tools/play_shots.sh "8-80" cut 2560 1440`，在播放模式里按镜头表逐镜头出帧（前后各留 8 帧余量）。
4. **房间镜头**：打开 `Room` 场景，`tools/sets.sh RoomFilm '["1-7,46,52,74,81,82",8,1,2560,1440,"cut","tv"]'`。
5. **合成**：`python -P tools/finish_film.py cut out.mp4 --crf 16 --captions docs/字幕.json`。负责软切与叠化、片名、片尾、换镜撕裂、大字歌词（`tools/lyric_type.py`）、中文与罗马音，并把响度标准化到 −14 LUFS。

`tools/redo_v3.sh` 是把以上几步串起来的完整脚本。

**注意**：很多脚本里写死了作者机器上的路径（`E:/sucai/...`），用之前请改成你自己的。

## 关于 AI 生成的内容

- 开场电视里的三段视频、对话卡和远楼窗里的十张插图：阿里云通义万相（wan3.0-video、wan2.7-image），提示词在 `gen/`。
- 纸面布景贴图（楼、天空、窗里的剪影等）：MiniMax image-01 生成，再由 `tools/make_stage_art.py` 等脚本处理。
- 六个动物头纸面具：MiniMax 出图，再经百炼 Tripo 转成三维模型，用 `tools/decimate_head.py` 减面。

## 许可

- 代码（`tools/`、`unity/AgentScripts/`、`unity/Assets/**/*.cs`、着色器）：MIT，见 `LICENSE`。
- 作者自制的文档、设计图、数据与脚本生成的贴图：CC BY 4.0，见 `LICENSE-ASSETS.md`。
- 第三方素材按各自的许可，见 `THIRD_PARTY.md`。
- 歌曲《生きていたんだよな》及其歌词的版权归あいみょん及相关权利人所有，本仓库不包含也不授权使用。
