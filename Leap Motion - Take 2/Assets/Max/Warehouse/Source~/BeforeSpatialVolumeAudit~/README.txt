WAREHOUSE — 凸出建筑、平平台与半管坡面修正（第6版）

场景：Assets/Max/WarehouseRecreation.unity
环境预制体：WarehouseEnvironment.prefab
最新对照：RouteReview.html
验证：validation.txt
相机坐标：Source~/ProportionCameraPoses.csv
原视频：https://www.youtube.com/watch?v=wQyMJNRPjjE

V6 针对用户指出的三处结构错误：
1. 门台由平墙改成真实凸出建筑。
   正面Z=-18.8（原主墙线-21），侧面X=-14.6延伸到Z=-28。
   两层窗、立柱、下部橙色栏栅，正面双门、门灯、0.9m高门台、三级踏步。
   侧栏栅底2.35m、顶3.55m，与侧面平台连接。尺寸均为截图推断。
2. 北端局部高台改为水平顶面，约4.1m宽、0.98m深、高3.8m。
   左侧窗翼加宽，箱子放到平顶平台上。
   半管南北占地从19.8m缩为13.46m，南缘仍为Z=-6.6、北缘约6.86；
   向东收进，西缘约X=5.33。扶手正前方破损小台移至后场偏东：
   约(12.3,0,13.5)。扶手前方与右侧恢复平地。
3. 金属通道坡顶开口改为展开形状，灰色底部宽约3.54m、顶部约7m；
   格栅宽约1.90m，金属顶1.72m，木坡顶3.105m，木坡弧段延长。
   通道中心Z=0.132；办公室随半管占地调整，仍在东端上方。
   17_ChannelWest_0152_Tagged.png为同一网格临时切换红色材质的渲染；
   10_ChannelWest_0152.png为基础木色。未实现动态Graffiti占领玩法。

本轮主要使用已有原始截图与新指出的局部对照；没有宣称重新遍历整个视频。
V5的7个起点投影锚点不覆盖上述结构，不能当成整场一致的证据。
太阳改为从东侧低角度照入，使内侧弧面能接到侧窗日光；
光照、材质与窗口细节仍有偏差，未做生产烘焙。

机位
主相机仍为Main Camera - P1 start 0132，位置(-11.34,1.96,10.30)，
旋转(13.77,166.67,0)，垂直FOV64；Game View为880×1000。
1:32、1:33、1:35、1:38、1:39机位沿用V5。通道机位根据更新后的
通道中心调整，1:52为(16.3,2,1.55)，旋转(18.5,258,0)。
其余相机见Source~/ProportionCameraPoses.csv；Hierarchy中有9个停用检查相机。
18_LoadingProjection.png是看清建筑侧面与门台的独立近景，不是视频同机位图。

验证
保存后重开，网格/材质/资产依赖、70处弧面、243处通道支撑、
5处展开坡顶横向支撑、6处原平台支撑、3级新台阶、
4处水平高台、3处开放地面检查通过。详见validation.txt。
82网格、107358三角形、45静态碰撞体。红色临时材质在保存前恢复。
这些检查不是相似度认证。轮廓、比例仍是估计，窗格、涂鸦、老化、
灯光与跟随相机还不同；未测试滑板和Leap Motion实机手感。

维护
原Assets/Max/MaxScene.unity未替换。
本轮修改前备份Source~/BeforeStructureDetailAudit~；旧V5报告、预览、场景、
网格、材质、预制体和脚本均保留。更早备份也保留。
Source~不参与正常Unity编译。Build会覆盖生成场景和生成资产；手工编辑请另存。
unity command script run --caller plugin --skill unity-cli --file Assets/Max/Warehouse/Source~/WarehouseBuilder.cs --entry WarehouseBuilder.Build
unity command script run --caller plugin --skill unity-cli --file Assets/Max/Warehouse/Source~/WarehouseChecks.cs --entry WarehouseChecks.Run
unity command script run --caller plugin --skill unity-cli --file Assets/Max/Warehouse/Source~/WarehouseFrameCalibration.cs --entry WarehouseFrameCalibration.ConfigureGameView
unity command script run --caller plugin --skill unity-cli --file Assets/Max/Warehouse/Source~/WarehouseFrameCalibration.cs --entry WarehouseFrameCalibration.Measure

