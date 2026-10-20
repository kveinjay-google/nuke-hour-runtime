# NUKE HOUR Runtime

不附带商业游戏资产的 Unity iOS 运行程序与资源导入器源码。官网：https://nukehour.com

## 包含什么

资源网页导入与文件导入、完整性检查、资源加载适配、iOS 平台适配、可设置的触控按钮、卫星布局与长按换位、陀螺仪开关和灵敏度。

**本仓库不包含官方地图、人物、贴图、动画、音效、音乐、关卡脚本、游戏预览图、AssetBundle 或原版安装文件。** 用户必须自行准备具有合法使用权的资源。无资源时只能使用启动器。此项目不是任何商业游戏的官方产品。

## 当前发布边界

这是去资产、去第三方未核清依赖的源码发布，不是克隆后立即可编译的完整 Unity 工程。原工程中第三方源码/二进制的许可不能由根目录 Apache 许可证替代，因此本快照不分发 NGUI、RnM UI、DOTween、SLua、Unity Standard Assets 和其他第三方插件；详见 [依赖说明](docs/DEPENDENCIES.md)。

历史私有工程已在 iPad Air 运行；这不代表本精简源码快照已完成 Unity 编译。本仓库仅声明实际执行的托管测试和发布检查结果。

## 开发与构建

使用 Unity **2020.1.0f1 / 2ab9c4179772**，iOS Build Support、IL2CPP、ARM64、Metal。依赖须从各自合法来源自行安装，保留其许可证。原始资源必须在独立的自用工程中转换，不可加入本仓库。当前导入格式是 METEOR_CONTENT_V1，不支持直接导入任意 PC 安装目录。

运行导入器单元测试：

```sh
python3 Tools/ResourceLauncher/run_tests.py
python3 Tools/check_public_release.py
```

独立资源工程使用 MeteorResourceBuild.MakeOwnedContent 生成 content.manifest、resources.bundle、scenes.bundle、resources.index。启动器在同一 Wi-Fi 中提供网页上传入口，无验证码；HTTP 仅适合可信局域网，使用后关闭导入页面。

发布构建由 MeteorResourceBuild.BuildResourceFree 生成空启动器场景，要求 METEOR_RELEASE_STAGING=YES 和新的 METEOR_RESOURCE_FREE_OUTPUT。程序自身不嵌入游戏资源。恢复依赖与自有启动器字体/插画后再构建，不能复制原版 Resources 目录到发布工程。

## IPA

网盘发布包中的 IPA 为不含游戏资产的 ARM64 导入器；公共版未签名，需要使用自己的 Apple 开发账号签名后安装。不附带开发者证书、私钥或设备描述文件。编译成功不代表所有玩法与触控已经验收。

## 许可与来源

继承上游 Apache-2.0，保留 LICENSE 与 NOTICE。第三方依赖不被重新许可。本快照来自 Meteor_original_ios，基线 fd705f38c8f9d0a027d7ac7bc17763fb72bbccfb，并包含后续移植修改。原仓库 Git 历史未复制，防止历史资产泄漏。AI 辅助开发使用 OpenAI Codex；本仓库不包含 AI 生成插画或商业游戏美术。
