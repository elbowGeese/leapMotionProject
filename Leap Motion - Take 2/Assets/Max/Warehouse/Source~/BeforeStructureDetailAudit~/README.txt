WAREHOUSE — 起点比例与路线逐张校正（第5版）

场景：Assets/Max/WarehouseRecreation.unity
环境预制体：Assets/Max/Warehouse/WarehouseEnvironment.prefab
最新对照：RouteReview.html（参考/Unity并排、透明度叠图、V4/V5切换）
验证：validation.txt
相机坐标：Source~/ProportionCameraPoses.csv
起点手工锚点测量：Source~/ProportionAnchors.csv
原视频：https://www.youtube.com/watch?v=wQyMJNRPjjE

本轮改动
用户指出起点看起来过宽，本轮以1:32左屏为主要标定图，并依次复看
1:33、1:35、1:38、1:39、1:46、1:52的路线节点。
大厅X缩到82%，主厅约45.92×42m；南端UBS前坡跨度约12.17m、高2.35m，
后方水平台与开间边界同步变形；南端厂牌中心高约9.30m，窄高坡高7.5m。
双跳台向南移8m、向西移约1.8m，宽约6.23m；红扶手高1.20m。
扶手北端拐弯朝东；北端箱子高坡向东移，并做高低唇之间的肩部过渡。
半管木坡高约2.59m；灰色通道顶约1.18m，开口宽5.2m、格栅宽2.8m。
日光改为西侧低角度斜射，南端端墙保持较暗，起点地面出现窗格阴影。
这些尺寸都是截图推断的重建估值，不是原游戏实测。

方向与结构关系
Unity Y向上，+Z称北、+X称东，只是本重建的说明坐标。
南端：UBS前坡→平台→RAG后坡→后退厂牌墙；正面左侧窄高坡，右侧双门L形低台。
北端：另一块厂牌位于实墙上，连续弧坡、箱子高坡与铁笼。
中央沟槽沿Z，轴线X约-3.28；双跳台位于西侧X约-15.74。
半管东西向滑行，唇口沿Z；中央灰色通道中心Z=3.3，沿X连到东墙下方。
办公室在东端上方，盒体X约18.86..22.96、Z约1.5..8.5。
上轮方向记录保留在Source~/RouteFrameAudit.csv；旧尺寸以本版为准。

相机与路线节点
主相机：Main Camera - P1 start 0132
位置(-11.34,1.96,10.30)，旋转(13.77,166.67,0)，垂直FOV 64。
Game View已选880×1000，对应分屏左半幅；宽屏比例会看到更多横向场地。
P1玩家起点标记约(-10.58,0.05,7.09)，朝向166.67度。
P2玩家起点约(14.2,0.05,-13.1)，朝向272度。
另有9个按时间命名的停用Camera，放在Video route anchors - estimated poses下：
07_P1_Start_0132、13_P1_Approach_0133、14_P1_Entry_0135、
15_P1_Return_0138、16_P1_Rail_0139、12_OfficePipe_0146、
10_ChannelWest_0152、11_ChannelOffice_0219、08_P2_Start_0132。
预览文件在Previews，主路线统一880×1000。环境预制体不包括相机和路线标记。
没有精确恢复原游戏跟随相机，也没有导入玩家模型。

验证与限制
保存后重新打开场景：79网格、105166三角形、43静态碰撞体。
70处弧坡法线/碰撞、243处连续通道支撑、6处平台支撑、凹口通行、
起点左右关系和资源依赖检查通过，具体见validation.txt。
1:32人工选取7个结构点：投影RMS误差V4约117.9px，V5约8.7px。
它们参与了调参，不是独立验证；不能推导全图或全场还原率。
1:38外坡轮廓和距离、1:46办公室遮挡、窗格/开间、材质及光照仍有差异。
Graffiti整块红蓝占领效果未加入。未测试滑板/Leap Motion玩法手感和生产光照。
当前为可编辑的环境重建，不是完整滑板游戏。

维护
Assets/Max/MaxScene.unity未替换。
本轮改动前备份：Source~/BeforeProportionAudit~，包括场景、模型、材质、
预览、脚本与报告。更早BeforeRouteAudit~等备份仍在。
Source~不参与正常编译。运行Build会覆盖生成场景及生成网格，手工编辑请另存。
unity command script run --caller plugin --skill unity-cli --file Assets/Max/Warehouse/Source~/WarehouseBuilder.cs --entry WarehouseBuilder.Build
unity command script run --caller plugin --skill unity-cli --file Assets/Max/Warehouse/Source~/WarehouseChecks.cs --entry WarehouseChecks.Run
unity command script run --caller plugin --skill unity-cli --file Assets/Max/Warehouse/Source~/WarehouseFrameCalibration.cs --entry WarehouseFrameCalibration.ConfigureGameView
unity command script run --caller plugin --skill unity-cli --file Assets/Max/Warehouse/Source~/WarehouseFrameCalibration.cs --entry WarehouseFrameCalibration.Measure

