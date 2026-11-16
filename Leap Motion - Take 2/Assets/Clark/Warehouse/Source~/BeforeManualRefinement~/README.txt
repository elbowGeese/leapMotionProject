WAREHOUSE — 连续坡面与端墙校正 V10
场景 Assets/Max/WarehouseRecreation.unity
预制体 WarehouseEnvironment.prefab；逐项图文对照 RouteReview.html
修改前完整备份 Source~/BeforeConnectedRampAudit~（V9）

用户确认与修改
- 整面北端墙中心X0细柱，宽约0.34m，凸出墙面约0.25m。
- 办公室下的坡沿既有Z方向，整端连续；金属格栅贴合同一坡面，不再有中央低槽。
- 大厅0，外侧坡顶+0.75m，坑底-2.25m；侧接坡接至坡顶带中线。
- 面对双高坡时左侧(+X)仅单层，直至侧墙；另一侧保留双层。
- 高坡顶后方延伸8.7m；中间带窗隔墙遮挡直视线，后方约3.5m可绕行连通。
- 高坡与外侧平台之间补铁网分隔。
- 办公室楼板顶3.9m，约6.86x12m，顶部连到屋顶。
以上尺寸为视觉估计，没有原游戏测量值。

验证
重开保存场景通过，55处曲面采样、204处坡面支撑采样通过。
核对薄柱真实凸出、连续坡面、凹坑无原平板遮挡、侧接坡法线、窗墙遮挡与后方通路。
办公室楼板、门台台阶、北端凸形平台与中央沟槽通过。
73网格、238634顶点、117670三角形、41静态碰撞体。
新保存相机31上层通道、32办公室接顶；26至29保留相同机位供前后对照。
详见validation.txt。

限制
本轮逐项处理用户结构修正，没有重新配准全部视频机位。
旧的1:32锚点误差已失效；WarehouseFrameCalibration.Measure现在仅导出当前相机坐标。
ProportionAnchors.csv保留为历史数据，不是V10准确率证明。
仍需精确比例、窗口标牌、材质与光照校准；未测滑板/Leap Motion操控与全路线滑行。

维护
Source~/WarehouseBuilder.cs生成器；Source~/WarehouseChecks.cs几何验证。
Build覆盖本场景生成资产；手工改动应另存场景及依赖网格。
原Assets/Max/MaxScene.unity未替换。

