WAREHOUSE — 从视频路线核对的独立环境重建（第4版）

场景：Assets/Max/WarehouseRecreation.unity
环境模型：Assets/Max/Warehouse/WarehouseEnvironment.prefab
最新图文对照：Assets/Max/Warehouse/RouteReview.html
本轮逐画面记录：Source~/RouteFrameAudit.csv
验证输出：validation.txt；预览：Previews/
原视频：https://www.youtube.com/watch?v=wQyMJNRPjjE

本轮针对用户提出的起点、金属通道、半管和办公室关系进行了重看与修正。
上一版确有结构误判：把两端相似的厂牌合并定位，将凹口放到了北端；
半管被错误旋转，金属带被错误放成南入口两条独立通道。以上结论已撤回。
当前以 RouteReview.html 为准。旧报告保留但明确标为历史记录。

观察方法
在浏览器暂停查看1:29–1:54的行进段，约每1–3秒取样，转身处加补帧；
另看2:18–2:26、2:43、2:53–2:54的反向和办公室视角。
不是逐个分析全部视频帧，也不是摄影测量。原始截图保存在Source~/References。
参考图在HTML里用CSS裁出左右分屏；没有把Unity渲染伪装成视频截图。

当前空间关系
Y向上；仅为说明定义+Z为北、+X为东。1 Unity unit约按1m建模，未得到原关卡实测。
主厅估计56×42m，檐口14m，屋脊17.8m；南端凹口局部到Z=-28。
南端：UBS前坡 → 水平台 → RAG后坡 → 后退厂牌墙；正面看左侧有窄高坡，右侧双门有L形低台。
北端：另一块厂牌位于实墙上；长弧坡、局部高唇、铁笼和叠箱，和南端开间不同。
中央：X约-4，沿Z的真实维修沟槽与红色扶手；西侧X约-17为双跳台及积水区。
半管：东侧，东西向滑行；两端弧坡唇口沿Z，中央平底在X约12..22.6。
金属带：一条连续带沿X贯穿半管，中心Z=3.3，宽约2.8；西端X约3开放向主厅，
         经过外坡、低金属顶、内坡和平底，到东端X约27.4的办公室下方。
         金属坡顶约2.15m、木坡高约3.45m，均为估值。
办公室：悬在东墙X约23..28、Z约1.5..8.5，上方盒体、下方支架和沿墙红管。
西墙：低平台处留出空间，不再与沿墙弧坡重叠；补出靠近通道西端的低位门洞与栏杆。

起点与检查相机
Hierarchy / Video route anchors - estimated poses
P1 0132 Start - orange - facing entry recess：(-9.4,0.05,9.5)，朝南略偏东。
  左侧中央扶手，右侧双跳台，前方南端凹口；位置和朝向均是重建估计。
P2 0132 Start - dark shirt - south corridor：(17,0.05,-13)，朝西略偏北。
  左侧南墙弧坡，右侧半管外坡；这个起点在半管外侧走廊。
另外提供沿路线命名的节点，以及4个停用的检查相机：
Camera 0132 P1 start / Camera 0132 P2 start / Camera 0152 Channel west / Camera 0219 Channel office。
相机从人物后方估计构图；没有原游戏的焦距、动态跟随位置或角色模型。
主相机仍为全场总览。环境预制体不包含这些检查相机和路线标记。

当前预览
01_ReferenceOverview：全场；02_CentralRail：中央沟槽；03_Halfpipe：半管与办公室。
04_LayoutTop：隐藏屋顶的俯视；05_SouthRecesses：南端开间；06_PlatformInside：平台内部。
07_P1_Start_0132、08_P2_Start_0132：两起点；09_P1_North_0139：北端箱子高坡。
10_ChannelWest_0152：通道向西；11_ChannelOffice_0219：反向看办公室；12_OfficePipe_0146：东墙红管。
旧文件名05_NorthRecesses已停用；正确方向以05_SouthRecesses为准。

验证与限制
重新打开保存场景，验证网格、材质、曲面碰撞法线、连续金属带支撑、平台通行、
两端厂牌分离、起点左右关系和资源依赖。具体计数以validation.txt为准。
这些检查证明当前几何有效，不等于与参考逐像素一致。
建筑宽高、设施距离、平台后退量、坡道半径、相机和画面透视仍需要更精确标定。
材质老化、涂鸦、窗格、杂物、光照与原作有明显差异；不报告未经标定的还原百分比。
红蓝整块坡面属于Graffiti占领效果，本环境保留木色。涂鸦和纹理是风格重建。
未新增滑板玩法、人物、计分或Leap Motion控制器；未验证实机手感，也没有生产光照烘焙。
没有提取原游戏模型。此次几何使用确定性网格；此前纹理使用imagegen，Hyper3D未调用。

维护
Assets/Max/MaxScene.unity未替换。当前重建场景和资产可以直接编辑，无需运行生成器。
Source~不参与Unity正常编译。主动运行Build会覆盖这个生成场景及生成网格；手工编辑请先另存。
unity command script run --caller plugin --skill unity-cli --file Assets/Max/Warehouse/Source~/WarehouseBuilder.cs --entry WarehouseBuilder.Build
unity command script run --caller plugin --skill unity-cli --file Assets/Max/Warehouse/Source~/WarehouseChecks.cs --entry WarehouseChecks.Run
本轮改动前备份：Source~/BeforeRouteAudit~（场景、预制体、网格、材质、预览、脚本及报告）。
更早备份：BeforeLayoutAudit~、BeforeRecessAudit~。旧VideoFrameAudit.csv含已撤回的方向推断，仅作历史记录。

项目兼容性（此前完成，本轮未再修改）
已加入Unity官方com.unity.pipeline 1.0.0编辑器控制包。
金币检测脚本此前改为可选SkateboardController类型检测，保留collectorRoot手动指定路径；
此前已通过金币预制体与三类触发检测。该部分不属于本轮场景位置修正。
