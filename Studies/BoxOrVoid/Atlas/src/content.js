export const meta = {
  n_tut_1: { title:'先认识两种空间', role:'建立规则', tags:['颜色切换'], question:'切换改变的是控制权，还是整张地图？', tech:'控制色与物体颜色分离', key:0, checkpoints:[6,13] },
  n_tut_2: { title:'把同伴变成桥', role:'引入协作', tags:['角色搭桥','目标次序'], question:'已经到终点的角色，为什么还要回来？', tech:'顶层覆盖与目标重新计算', key:5, checkpoints:[4,6,10,11] },
  n_tut_3: { title:'互相铺路', role:'规则回收', tags:['角色搭桥','交替协作'], question:'两个人怎样轮流成为对方的路？', tech:'同格堆叠，不能只存一个 occupant', key:7, checkpoints:[7,13,21] },
  n_tut_4: { title:'拐弯后的交接', role:'空间迁移', tags:['角色搭桥','交替协作'], question:'几何上相邻的格子，就一定能直接抵达吗？', tech:'格子通行取决于当前顶层', key:7, checkpoints:[7,16,22] },
  n_tut_5: { title:'绕行也是工作', role:'组合应用', tags:['角色搭桥','目标次序'], question:'同伴现在站哪里，才能及时赶到下一处？', tech:'角色位置承担临时地形职责', key:8, checkpoints:[8,14,23] },
  n_tut_6: { title:'桥头不是终点', role:'组合应用', tags:['角色搭桥','目标次序'], question:'过桥之后，应先到终点，还是先去接同伴？', tech:'完成状态不能替代依赖规划', key:11, checkpoints:[11,24,31] },
  n_tut_7: { title:'把交接串成路线', role:'长链综合', tags:['角色搭桥','交替协作'], question:'能否从最后一个交接点反推整条路线？', tech:'多次切换与撤销的确定性', key:26, checkpoints:[12,26,40] },
  n_tut_8: { title:'长路线，少规则', role:'章节综合', tags:['角色搭桥','交替协作','工具无目标'], question:'只有一个目标时，另一角色的价值是什么？', tech:'不要硬编码每种颜色必须各有目标', key:78, checkpoints:[25,61,78,114] },
  n_box_1: { title:'推自己的箱子', role:'建立规则', tags:['同色推动'], question:'箱目标与角色目标，用什么区分？', tech:'目标同时匹配颜色、类型与覆盖数量', key:0, checkpoints:[5,13] },
  n_box_2: { title:'桥要接住箱与人', role:'规则迁移', tags:['角色搭桥','移动地形','叠放'], question:'箱子已经走开，桥下的角色为什么仍动不了？', tech:'推手也会覆盖桥下角色', key:22, checkpoints:[13,22,23,27] },
  n_box_13: { title:'两只箱，两种用途', role:'双向协作', tags:['移动地形','工具无目标'], question:'没有目标的箱子，是否仍是解题关键？', tech:'物体的功能不由目标数量决定', key:15, checkpoints:[3,15,19] },
  n_box_5: { title:'先送远，再推回', role:'反转直觉', tags:['移动地形','叠放','换边'], question:'目标明明就在身后，为什么先把箱推向远处？', tech:'覆盖改变箱子的通行语义', key:8, checkpoints:[4,8,15,19] },
  n_box_4: { title:'让完成暂时失效', role:'状态理解', tags:['目标次序','叠放'], question:'已完成的目标，还能成为别人的路吗？', tech:'目标是当前状态谓词，不能永久锁定', key:18, checkpoints:[16,18,26] },
  n_box_6: { title:'箱子就是路面', role:'地形组合', tags:['移动地形','工具无目标','换边'], question:'箱子没有终点，那这一关究竟在推什么？', tech:'把推动计划与关卡胜利条件解耦', key:24, checkpoints:[13,21,24,27,33] },
  n_box_3: { title:'最后一格是自己', role:'多物体组合', tags:['同色箱链','移动地形','工具无目标'], question:'两只箱摆好了，为什么仍然缺一格路？', tech:'角色和箱子共享通行判定', key:26, checkpoints:[6,20,23,26] },
  n_box_7: { title:'在箱子之间夹一个人', role:'叠层发现', tags:['同色箱链','三层叠放','拆链'], question:'同色箱子如何叠在同一格，再重新分开？', tech:'只判断最高层，保存完整底层状态', key:17, checkpoints:[5,16,17,21,28] },
  n_box_8: { title:'同一机关，换退出顺序', role:'分支变式', tags:['三层叠放','拆链','换边'], question:'窄一格，为什么会改变整套交接顺序？', tech:'空间约束变化不必新增规则', key:17, checkpoints:[17,25,26,30] },
  n_box_9: { title:'让角色托住箱子', role:'双向组合', tags:['移动地形','叠放','换边'], question:'推箱的站位，也能由另一个角色提供吗？', tech:'被覆盖角色可以保留在终局', key:37, checkpoints:[19,28,37,38] },
  n_box_10: { title:'拆开交叉的箱链', role:'系统综合', tags:['同色箱链','拆链','叠放'], question:'怎样让一整条箱链只移动其中一只？', tech:'依赖由当前顶层重新求解，不能永久绑定箱链', key:9, checkpoints:[4,9,13,21,26] },
  n_box_11: { title:'不对称的拆链', role:'分支综合', tags:['同色箱链','拆链','换边'], question:'拆完一条链后，还怎样重新进入推动位置？', tech:'原子推动与覆盖顺序同时可撤销', key:8, checkpoints:[6,8,11,20,28] },
  n_box_12: { title:'为中间状态找位置', role:'长链综合', tags:['移动地形','叠放','换边'], question:'两只箱的临时存放位置，怎样互相成全？', tech:'快照必须保留物体身份与相对层序', key:48, checkpoints:[28,40,48,54,58,62] },
};

export const labCases = [
  {name:'角色搭桥',id:'n_tut_2',step:5,cell:[5,2],lead:'让白角色向右走一步。',detail:'黑角色覆盖白墙，成为白角色可走的顶面。再切到黑色并尝试移动，观察“被覆盖”的限制。',insight:'一次换面，同时改变通行与行动权。'},
  {name:'箱子造路',id:'n_box_6',step:23,cell:[4,3],lead:'向上推动，把白箱放到黑角色上。',detail:'这里没有箱目标。箱子负责把白色地形变成黑角色可以经过的路，推动只是改造地形的手段。',insight:'对象的价值，不必由终点定义。'},
  {name:'三层叠放',id:'n_box_7',step:17,cell:[2,3],lead:'点击高亮格，读取从底到顶的三层。',detail:'白箱—黑角色—白箱。切到黑色尝试移动，会因头上还有一层而失败；只有最上面的白箱参与外部通行判断。',insight:'视觉可隐藏底层，逻辑必须完整保存。'},
  {name:'目标会失效',id:'n_box_4',step:16,cell:[3,5],lead:'切到黑色，再向右走一步。',detail:'白箱刚到箱目标；黑角色站上去后，上方不再恰好只有一件匹配物体，完成勾随之取消。',insight:'目标是实时谓词，不是一次性开关。'},
];

export const architecture = [
  {name:'输入请求',file:'StudyInput.cs',original:'GameInput / InputAction',contract:'按键与长按 → Command',why:'把方向、切换与历史操作变成离散意图。同一条路径可用于真实键盘与隔离回放。',design:'动作重复太快会越格；太慢会让长路线显得拖沓。参数应与 0.1 秒位移动画共同调试。',risk:'把 KeyDown 散落在各个物体，会让撤销和自动回放走不同规则。',origin:'原作通过 InputAction 分发；移动中切换会被忽略，撤销请求会等待稳定帧。'},
  {name:'规则约束',file:'PuzzleSession.cs',original:'PlayerController / Box',contract:'State + Command → 可行性',why:'先确认当前角色位于顶层，再读目标格最上层对象的颜色。同色箱可以推，同色墙与灰墙不能走。',design:'“异色成为表面”同时应用于角色、箱子、墙。保持一致，玩家才可凭理解推导未知情况。',risk:'只按底板颜色碰撞，会让角色搭桥和箱子造路失效。',origin:'原作以碰撞查询和 Sprite sortingOrder 识别最高层，并在 PlayerController / Box 中处理移动。'},
  {name:'依赖计划',file:'PuzzleSession.cs',original:'Box.TryPush',contract:'推动依赖 → 远端优先的计划',why:'同色箱链递归检查链尾。只要一处受阻，整次推动拒绝；所有依赖都可行才统一移动。',design:'玩家把连推感知为一次动作，因此一次 Z 也必须恢复整条链。',risk:'先移动近端箱再尝试链尾，会在失败时留下半次推动。',origin:'原作 Box.TryPush 处理同色推动；异色对象留在下方，新的动体从上层进入。'},
  {name:'状态与历史',file:'PuzzleSession.cs',original:'GameManager / MoveState',contract:'快照 + 控制色 + 层序',why:'状态保存所有物体的位置、颜色与相对深度；撤销包含切换。重置保存恢复点及此前历史。',design:'玩家愿意尝试，依赖的是可预测的撤回。重置之后仍能反悔，会降低长关的探索成本。',risk:'用反向移动实现撤销，无法恢复覆盖次序和控制色；连续重置还会覆盖旧恢复点。',origin:'原作 GameManager 管理移动快照、撤销栈及独立 UndoResetCheckpoint；这不是声称原作采用纯 Core 分层。'},
  {name:'表现与反馈',file:'BoardView.cs',original:'ObjectSpriteManager / UnderIndicator',contract:'ActionResult → 动画 / 标记 / 声音',why:'动画投影权威状态，不能决定能否推动。顶层只显示正下方的一个标记；目标勾优先。',design:'没有层标记，玩家无法辨认被盖住的角色；把所有底层图标画出来，又会让三层格不可读。',risk:'移动未结束就显示新标记，或让动画回调改写权威状态，会产生提前完成与撤销竞态。',origin:'原作 UnderIndicator 检查顶层与目标，并按约 0.3 秒显现；ObjectSpriteManager 按控制色更新展示。'},
  {name:'内容与工具',file:'StudyAssets.cs',original:'LevelDatabase / LevelLoader',contract:'JSON + Catalog → 同一玩法入口',why:'布局、显示编号、解锁关系与表现映射各司其职。所有 21 关加载到一个共享场景，工具和回放使用同一规则。',design:'主线 03 实际来自文件 n_box_13；按文件名排序会破坏教学。A/B/C 应在对应节点分支。',risk:'每关单做 Scene 会复制相机、输入与 UI；改一个规则得追着改多关。',origin:'原作包含数据化关卡、LevelDatabase、加载与选关组件；本复刻另建轻量工作台，不声称复制了原作编辑器。'},
];

export const soundNotes = {
  playerMove:['有效移动','行动','确认“这一格确实走成了”，以短触点维持格子节奏。','PlayerController → PlayPlayerMove；不以持续摩擦声填满思考时间。'],
  playerMoveBlocked:['移动受阻','行动','把“收到输入”与“成功行动”区分开，减少按键失灵的误解。','PlayerController → PlayPlayerMoveBlocked；受阻不增加撤销记录。'],
  playerSwitch:['切换控制','状态','给跨角色的控制权交接一个清晰听觉边界。','GameManager → PlayPlayerSwitch；颜色变化由画面同步承接。'],
  boxPush:['推动箱子','行动','与普通移动区分，让更重、更有后果的动作有独立触感。','SfxLibrary.boxPush 指向 impactWood_heavy_001，名称仅是素材名。'],
  goalFulfilled:['目标满足','状态','局部正反馈；完成一个目标不等于整关结束。','SfxLibrary.goalFulfilled 有资源；goalUnfulfilled 字段在当前库为空，不能虚构取消音效。'],
  undo:['撤回','历史','确认回退发生，而不是假装角色向相反方向走了一步。','原作 0.06 秒防连发；播放时音高随机 0.98–1.02。'],
  reset:['重置','历史','区分一步回退与整局恢复，给更大范围的状态改变独立提示。','原作音高随机 0.99–1.01；重置仍可通过检查点撤销。'],
  levelVanishTick:['关卡消散','状态','把结算拆成一串轻反馈，让离开棋盘成为可感知的过渡。','原作按消散进度播放短音；复刻在完成过渡中调节音高。'],
  uiNavigate:['菜单移动','界面','细小移动音为“当前选择变了”提供额外确认。','UiSfxPlayer 与玩法声分路；原作还对滑条 tick 做 0.03 秒节流。'],
  uiSubmit:['菜单确认','界面','让浏览和执行有不同落点；不要每个 hover 都使用确认音。','取自 UiSfxLibrary；这里展示文件与触发语义。'],
  selectionMove:['选关移动','界面','导航图中的一次有效移动应有轻而明确的反馈。','选关组件使用 click_001；支持方向导航的离散节奏。'],
  selectionMoveBlocked:['选关受阻','界面','在没有对应方向邻居时，解释为什么选择没有移动。','选关组件使用 click_002，不能与锁关确认完全混同。'],
  selectionSelect:['进入关卡','界面','从选关图进入棋盘，是一次比焦点变化更重的操作。','素材 levelSelect；是否可选还需状态和光标颜色共同表达。'],
  pauseOpen:['暂停开关','界面','把思考中的棋盘与菜单操作区分开。','暂停打开与关闭复用同一 pauseMenu 资源。'],
  theme:['欢迎章节音乐','音乐','为长时间观察提供连续底色，避免动作反馈成为唯一声音。','MusicLibrary.theme → Covert Affair_Loop；曲名并不构成风格分析的证据。'],
  boxWorld:['箱即是空章节音乐','音乐','随着机制章节更换听觉环境，同时维持可循环的陪伴感。','MusicLibrary.boxWorld → Firmament_loopable；这里只能证明章节资源映射。'],
};

export const lessons = [
  {title:'先证明一个发现，再搬动它的语境',from:'欢迎/02 → 箱子/02 → 箱子/08',text:'单格角色桥，变成要接住箱与人的桥，再变成箱—人—箱的三层结构。新增的是组合后果，不一定是新机制。',action:'为自己的机制写一条可观察的规则，再设计“认识 / 迁移 / 反转”三关。',level:'n_tut_2'},
  {title:'让中间状态也有设计价值',from:'箱子/04、05、06',text:'箱子可以先远离目标，已经满足的目标也可以暂时失效；一件物体还可以根本没有终点。局部完成不必锁死。',action:'画依赖图时先标临时站位、换边条件与退出路线，再摆终点。',level:'n_box_5'},
  {title:'可靠的撤销，是探索的基础设施',from:'欢迎/08 的长路线',text:'能从重置里撤回、能稳定恢复覆盖顺序，玩家才敢尝试。其成本落在状态设计，而不是额外做一个 Z 按钮。',action:'用一段含切换、叠放与推动的序列做往返验证，比较完整状态。',level:'n_tut_8'},
  {title:'工具应与产品共享同一套规则',from:'21 份 JSON → 一个共享场景',text:'编辑、检查、回放、求解都从相同 Definition 进入。工作台的“数据合法”只保证结构，不保证可解，更不保证好玩。',action:'用原始数据 + 有效回放 + 关键状态截图组成每关的最小验证包。',level:'n_box_7'},
];
