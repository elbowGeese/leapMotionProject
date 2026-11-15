WAREHOUSE — 根据截图重建的独立仓库滑板场

新场景：Assets/Max/WarehouseRecreation.unity
可复用环境模型：Assets/Max/Warehouse/WarehouseEnvironment.prefab
预览图：Assets/Max/Warehouse/Previews
验证记录：Assets/Max/Warehouse/validation.txt

本场景已保存为可直接编辑的 Unity 网格、材质和对象层级。
无需运行生成器，未替换 Assets/Max/MaxScene.unity。
建模方式：按参考关系构建的确定性网格；使用内置 imagegen 生成墙面、地面、木板和涂鸦纹理。
本次没有使用 Hyper3D：当前会话未发现可调用连接。

布局坐标与推断
1 Unity unit = 1 m；Y 向上。为便于说明，将厂牌墙定义为“北”侧（+Z）。
外壳约 56 x 42 m，X=-28..28，Z=-21..21；檐口高 14 m，屋脊高 17.8 m。
以上尺寸是以人物、门窗和设施相对比例估算，未取得原游戏实测数据。

北侧：厂牌居中，大面积窗格；贴墙弧坡高度约 2.6 m，左侧有 3.8 m 的高台延伸。
中央：X=-4、Z=-13..7 的维修沟槽，宽 2 m、深约 0.8 m；红色扶手沿东缘延伸。
左侧：X=-17 的成对小跳台，中间留出空隙；更靠西墙有积水、碎块和散落木板。
右侧：X=3..24.8、Z约=-7.66..14.26 的半管设施，内侧弧坡高 3.45 m。
       二次核对后已转向：两条连续长唇口沿 X，与中央红栏杆垂直；平底沿 X 通向东侧悬挑办公室。
       移除了第一版误放在半管里的金属通道。以上尺寸仍是重建估值，不是已核实的原关卡尺寸。
后方中部：低矮的破损金字塔形设施。
东墙：半管上方的悬挑办公室、窗框、斜撑。
南侧：入口坡道区。三段弧坡之间设置两条金属下坡通道；精确间距和入口后方房间仍待更多画面确认。
屋顶：横跨厂房的钢桁架、纵向檩条、中央天窗、悬吊工业灯。

证据与置信度
主要基准是用户第 7 张全景；第 3、4、10、16 张确认沟槽与红扶手；
第 9 张提供入口金属通道和小跳台的近景；第 11、12、13 张提供积水区关系。
原视频：https://www.youtube.com/watch?v=wQyMJNRPjjE （1:30 起，浏览器中核对了部分画面）。
Source~/References 保留了关键原图。
厂房、沟槽、双跳台、半管、悬挑办公室之间的相对分区有图片依据。
背向镜头的入口、被遮挡的边缘、精确跨度和坡道半径属于推测补全。
视频中的整块红色/蓝色坡面按 Graffiti 模式的占领效果处理；场景保留自然木色。
涂鸦是参考风格再创作，未逐像素复刻原游戏贴图。没有提取原游戏模型。

编辑与使用
Hierarchy 的 Warehouse - reference reconstruction 下按设施分组。
每组设施是独立网格对象；坡道使用匹配的非凸 MeshCollider。
装饰物不参与碰撞；主地板为沟槽保留了真实空洞。
WarehouseEnvironment.prefab 可拖入其他场景；相机和灯光单独保存在新场景中。
Player spawn - environment only 是预留位置，本次未添加玩家、滑板、计分或 Leap Motion 控制。

Previews/01_ReferenceOverview.png：厂房总览。
Previews/02_CentralRail.png：中央红色扶手和沟槽。
Previews/03_Halfpipe.png：从半管开口向东侧悬挑办公室看的视图。
Previews/04_LayoutTop.png：隐藏屋顶后的俯视布局。

重建脚本保留在 Source~（Unity 不会将其编译进游戏）。
注意：主动执行 Build 会重新创建并覆盖这个生成场景及其网格，手工编辑前请另存版本。
unity command script run --caller plugin --skill unity-cli --file Assets/Max/Warehouse/Source~/WarehouseBuilder.cs --entry WarehouseBuilder.Build
unity command script run --caller plugin --skill unity-cli --file Assets/Max/Warehouse/Source~/WarehouseChecks.cs --entry WarehouseChecks.Run

项目兼容性
添加了 Unity 官方 com.unity.pipeline 1.0.0 编辑器控制包以完成本次创建和检查。
启动时原金币脚本依赖缺失的 Fingerboard.SkateboardController 而无法编译；
将自动检测改为可选类型检测，保留 collectorRoot 手动指定路径。
已有金币预制体及“普通刚体拒绝 / 手动 collectorRoot 接受 / trigger 拒绝”检查已通过。

限制
这是根据参考重建的环境模型，不是原游戏关卡文件；整体关系、材质风格和主要设施已重建。
未验证实机 Leap Motion 或滑板控制器手感；没有烘焙生产用光照贴图。
精确原关卡尺寸、背面构造、微小杂物以及涂鸦笔画仍有差异。

二次布局审查
详细对照：LayoutReview.html。
结论：第一版并不相同；已修正半管方向和金属通道所属区域，但不能宣称整体位置已经 1:1 对齐。
原图与预览相机尚不能同时对齐多个固定参照点；不报告未经标定的“还原百分比”或原关卡米制误差。
双跳台与栏杆间距、后方低跳台偏移、建筑长宽比例、窗墙及厂牌比例仍列为未通过精确位置核对。
修改前场景、预制体、网格和预览保存在 Source~/BeforeLayoutAudit~。
