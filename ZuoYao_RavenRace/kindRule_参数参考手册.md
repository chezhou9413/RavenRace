# kindRule XML 参数参考手册

> 适用于 ChezhouLib 的 `kindRule` DefModExtension，用于在 PawnKindDef 上配置角色的外观、技能、能力等。

---

## 目录

1. [基本结构](#1-基本结构)
2. [基础参数](#2-基础参数)
3. [Boss 配置](#3-boss-配置)
4. [技能设置 (skillSettings)](#4-技能设置-skillsettings)
5. [初始健康状态 (setinitHedif)](#5-初始健康状态-setinithedif)
6. [能力 (addAbility)](#6-能力-addability)
7. [特性 (traitName)](#7-特性-traitname)
8. [BodyAddon 规则 (bodyAddonRules)](#8-bodyaddon-规则-bodyaddonrules)
9. [服装材质替换 (ApparelStuffRules)](#9-服装材质替换-apparelstuffrules)
10. [渲染节点规则 (renderNodeRules)](#10-渲染节点规则-rendernoderules)
11. [完整案例](#11-完整案例)

---

## 1. 基本结构

`kindRule` 作为 `DefModExtension` 挂载在 `PawnKindDef` 上：

```xml
<PawnKindDef>
  <defName>MyPawnKind</defName>
  <!-- ... 其他 PawnKindDef 参数 ... -->
  <modExtensions>
    <li Class="ChezhouLib.Extension.kindRule">
      <!-- kindRule 参数写在这里 -->
    </li>
  </modExtensions>
</PawnKindDef>
```

---

## 2. 基础参数

| 参数 | 类型 | 说明 | 示例 |
|------|------|------|------|
| `kindName` | string | 角色显示名称 | `<kindName>暗影渡鸦</kindName>` |
| `kindDesName` | string | 角色描述名称 | `<kindDesName>渡鸦族精英战士</kindDesName>` |
| `hairProDefName` | string | 指定发型 DefName | `<hairProDefName>RavenHair_A</hairProDefName>` |
| `Gender` | string | 性别（Male / Female） | `<Gender>Female</Gender>` |
| `kindAge` | int | 指定年龄 | `<kindAge>25</kindAge>` |
| `BodyTypeName` | string | 体型名称 | `<BodyTypeName>Female</BodyTypeName>` |

### 示例

```xml
<li Class="ChezhouLib.Extension.kindRule">
  <kindName>暗影渡鸦</kindName>
  <kindDesName>渡鸦族精英战士</kindDesName>
  <Gender>Female</Gender>
  <kindAge>25</kindAge>
  <BodyTypeName>Female</BodyTypeName>
  <hairProDefName>RavenHair_A</hairProDefName>
</li>
```

---

## 3. Boss 配置

用于标记角色为 Boss 并配置专属 BGM。

| 参数 | 类型 | 说明 | 默认值 |
|------|------|------|--------|
| `isBoss` | bool | 是否为 Boss | false |
| `BossSongDef` | SongDef | Boss 战斗音乐的 SongDef | 无 |
| `BossSongPos` | int | 音乐开始播放的位置（秒） | 0 |

### 示例

```xml
<boss>
  <isBoss>true</isBoss>
  <BossSongDef>MyBossBGM</BossSongDef>
  <BossSongPos>5</BossSongPos>
</boss>
```

---

## 4. 技能设置 (skillSettings)

为角色指定固定的技能等级和热情度。

| 参数 | 类型 | 说明 | 示例 |
|------|------|------|------|
| `skillDef` | SkillDef | 技能定义 | `Shooting`、`Melee`、`Construction` 等 |
| `minLevel` | int | 最低技能等级 | `10` |
| `maxLevel` | int | 最高技能等级 | `15` |
| `passion` | Passion | 热情度 | `None`、`Minor`、`Major` |
| `xpSinceLastLevel` | float | 当前等级已积累的经验值 | `0` |

### 示例

```xml
<skillSettings>
  <li>
    <skillDef>Shooting</skillDef>
    <minLevel>12</minLevel>
    <maxLevel>16</maxLevel>
    <passion>Major</passion>
    <xpSinceLastLevel>0</xpSinceLastLevel>
  </li>
  <li>
    <skillDef>Melee</skillDef>
    <minLevel>8</minLevel>
    <maxLevel>12</maxLevel>
    <passion>Minor</passion>
    <xpSinceLastLevel>0</xpSinceLastLevel>
  </li>
  <li>
    <skillDef>Intellectual</skillDef>
    <minLevel>5</minLevel>
    <maxLevel>10</maxLevel>
    <passion>None</passion>
    <xpSinceLastLevel>0</xpSinceLastLevel>
  </li>
</skillSettings>
```

---

## 5. 初始健康状态 (setinitHedif)

角色生成时自动添加的 Hediff（健康状态/植入物等）。

| 参数 | 类型 | 说明 | 示例 |
|------|------|------|------|
| `hediffToAdd` | HediffDef | 要添加的 Hediff 定义 | `Hediff_MyBuff` |
| `partDef` | BodyPartDef | 目标身体部位定义（可选） | `Heart`、`Brain` |
| `partLabelContains` | string | 按标签名模糊匹配身体部位（可选） | `"left hand"` |

> `partDef` 和 `partLabelContains` 都为空时，Hediff 将添加到全身（wholebody）。

### 示例

```xml
<setinitHedif>
  <!-- 全身 buff -->
  <li>
    <hediffToAdd>Raven_BloodlineBuff</hediffToAdd>
  </li>
  <!-- 指定身体部位 -->
  <li>
    <hediffToAdd>Raven_EyeEnhancement</hediffToAdd>
    <partDef>Eye</partDef>
  </li>
  <!-- 模糊匹配部位 -->
  <li>
    <hediffToAdd>Raven_ArmBuff</hediffToAdd>
    <partLabelContains>left arm</partLabelContains>
  </li>
</setinitHedif>
```

---

## 6. 能力 (addAbility)

角色生成时自动获得的能力。

### 示例

```xml
<addAbility>
  <li>Raven_ShadowStep</li>
  <li>Raven_DarkPulse</li>
</addAbility>
```

---

## 7. 特性 (traitName)

角色生成时自动添加的特性（Trait）。

### 示例

```xml
<traitName>
  <li>Raven_NightVision</li>
  <li>Psychopath</li>
</traitName>
```

---

## 8. BodyAddon 规则 (bodyAddonRules)

用于替换 HAR (Humanoid Alien Races) 的 BodyAddon 贴图路径、颜色和偏移。

| 参数 | 类型 | 说明 | 示例 |
|------|------|------|------|
| `BodyAddonName` | string | BodyAddon 的 path 名称（用于匹配） | `"Races/Raven/Wing/RavenWing_LayerA"` |
| `ReplacePath` | string | 替换后的贴图路径 | `"Races/Raven/Special/Wing/SpecialWing"` |
| `fixedColor` | Color | 固定颜色（RGBA），Color.clear 表示不替换 | `(1, 0, 0, 1)` 为红色 |
| `offset` | DirectionalOffset | 位置偏移（HAR 格式） | 见下方 |

### 示例

```xml
<bodyAddonRules>
  <!-- 替换翅膀贴图 -->
  <li>
    <BodyAddonName>Races/Raven/Wing/RavenWing_LayerA</BodyAddonName>
    <ReplacePath>Races/Raven/Special/ZuoYao/Wing/ZuoYao_Wing_LayerA</ReplacePath>
  </li>
  <!-- 替换头发并改变颜色 -->
  <li>
    <BodyAddonName>Races/Raven/Hair/RavenHair_A</BodyAddonName>
    <ReplacePath>Races/Raven/Special/ZuoYao/Hair/ZuoYao_Hair</ReplacePath>
    <fixedColor>(0.8, 0.1, 0.1, 1)</fixedColor>
  </li>
</bodyAddonRules>
```

---

## 9. 服装材质替换 (ApparelStuffRules)

强制指定角色穿着的服装使用特定材质。

| 参数 | 类型 | 说明 | 示例 |
|------|------|------|------|
| `Apparel` | string | 服装的 DefName | `"Raven_Apparel_Casual"` |
| `ReplaceStuff` | string | 替换材质的 DefName | `"DevilstrandCloth"` |

### 示例

```xml
<ApparelStuffRules>
  <li>
    <Apparel>Raven_Apparel_Casual</Apparel>
    <ReplaceStuff>DevilstrandCloth</ReplaceStuff>
  </li>
  <li>
    <Apparel>Raven_Apparel_Qipao</Apparel>
    <ReplaceStuff>Synthread</ReplaceStuff>
  </li>
</ApparelStuffRules>
```

---

## 10. 渲染节点规则 (renderNodeRules)

用于替换原生渲染节点（非 BodyAddon）的贴图、颜色和偏移。适用于 Head、Body、Hair 等原生节点。

| 参数 | 类型 | 说明 | 默认值 |
|------|------|------|--------|
| `tagDefName` | string | 匹配节点的 PawnRenderNodeTagDef 的 defName（优先匹配） | 无 |
| `debugLabel` | string | 匹配节点的 debugLabel（备选匹配） | 无 |
| `replacePath` | string | 替换贴图路径，为空则不替换贴图 | 无 |
| `fixedColor` | Color | 固定颜色（RGBA），`(0,0,0,0)` 即 Color.clear 表示不替换 | `(0,0,0,0)` |
| `offsetSouth` | Vector3 | 朝南时的位置偏移 `(x, y, z)` | `(0, 0, 0)` |
| `offsetNorth` | Vector3 | 朝北时的位置偏移 | `(0, 0, 0)` |
| `offsetEast` | Vector3 | 朝东时的位置偏移 | `(0, 0, 0)` |
| `offsetWest` | Vector3 | 朝西时的位置偏移 | `(0, 0, 0)` |
| `useOffset` | bool | 是否启用偏移 | `false` |

> **`tagDefName` vs `debugLabel`**：优先使用 `tagDefName` 精确匹配渲染节点标签（如 `"Head"`、`"Body"`、`"Hair"`）。当无法用 tagDefName 区分时，使用 `debugLabel` 进行备选匹配。

### 示例：替换头部贴图

```xml
<renderNodeRules>
  <li>
    <tagDefName>Head</tagDefName>
    <replacePath>Races/Raven/Special/ZuoYao/Head/ZuoYao_Head</replacePath>
  </li>
</renderNodeRules>
```

### 示例：替换贴图 + 固定颜色

```xml
<renderNodeRules>
  <li>
    <tagDefName>Hair</tagDefName>
    <replacePath>Races/Raven/Special/ZuoYao/Hair/ZuoYao_Hair</replacePath>
    <fixedColor>(0.9, 0.2, 0.2, 1)</fixedColor>
  </li>
</renderNodeRules>
```

### 示例：使用偏移

```xml
<renderNodeRules>
  <li>
    <tagDefName>Head</tagDefName>
    <replacePath>Races/Raven/Special/ZuoYao/Head/ZuoYao_Head</replacePath>
    <useOffset>true</useOffset>
    <offsetSouth>(0, 0.05, 0)</offsetSouth>
    <offsetNorth>(0, 0.05, 0)</offsetNorth>
    <offsetEast>(0.02, 0.05, 0)</offsetEast>
    <offsetWest>(-0.02, 0.05, 0)</offsetWest>
  </li>
</renderNodeRules>
```

### 示例：用 debugLabel 匹配

```xml
<renderNodeRules>
  <li>
    <debugLabel>Raven_SpecialOverlay</debugLabel>
    <replacePath>Races/Raven/Special/Overlay/NewOverlay</replacePath>
  </li>
</renderNodeRules>
```

---

## 11. 完整案例

以下是一个完整的 PawnKindDef 配置示例，展示了 kindRule 的所有参数：

```xml
<PawnKindDef ParentName="RavenKindBase">
  <defName>Raven_ZuoYao</defName>
  <label>佐瑶</label>
  <race>Raven_Race</race>
  <defaultFactionType>Raven_Faction</defaultFactionType>

  <modExtensions>
    <li Class="ChezhouLib.Extension.kindRule">
      <!-- 基础信息 -->
      <kindName>佐瑶</kindName>
      <kindDesName>渡鸦族的神秘祭司</kindDesName>
      <Gender>Female</Gender>
      <kindAge>28</kindAge>
      <BodyTypeName>Female</BodyTypeName>
      <hairProDefName>RavenHair_A</hairProDefName>

      <!-- Boss 配置 -->
      <boss>
        <isBoss>true</isBoss>
        <BossSongDef>Raven_BossBGM_ZuoYao</BossSongDef>
        <BossSongPos>3</BossSongPos>
      </boss>

      <!-- 技能设置 -->
      <skillSettings>
        <li>
          <skillDef>Shooting</skillDef>
          <minLevel>14</minLevel>
          <maxLevel>18</maxLevel>
          <passion>Major</passion>
          <xpSinceLastLevel>0</xpSinceLastLevel>
        </li>
        <li>
          <skillDef>Melee</skillDef>
          <minLevel>10</minLevel>
          <maxLevel>14</maxLevel>
          <passion>Minor</passion>
          <xpSinceLastLevel>0</xpSinceLastLevel>
        </li>
        <li>
          <skillDef>Intellectual</skillDef>
          <minLevel>15</minLevel>
          <maxLevel>20</maxLevel>
          <passion>Major</passion>
          <xpSinceLastLevel>0</xpSinceLastLevel>
        </li>
      </skillSettings>

      <!-- 初始 Hediff -->
      <setinitHedif>
        <li>
          <hediffToAdd>Raven_ZuoYao_Blessing</hediffToAdd>
        </li>
        <li>
          <hediffToAdd>Raven_EnhancedVision</hediffToAdd>
          <partDef>Eye</partDef>
        </li>
      </setinitHedif>

      <!-- 能力 -->
      <addAbility>
        <li>Raven_ShadowStep</li>
        <li>Raven_DarkPulse</li>
        <li>Raven_HealingRitual</li>
      </addAbility>

      <!-- 特性 -->
      <traitName>
        <li>Raven_NightVision</li>
        <li>Psychopath</li>
      </traitName>

      <!-- BodyAddon 替换规则 -->
      <bodyAddonRules>
        <li>
          <BodyAddonName>Races/Raven/Wing/RavenWing_LayerA</BodyAddonName>
          <ReplacePath>Races/Raven/Special/ZuoYao/Wing/ZuoYao_Wing_LayerA</ReplacePath>
        </li>
        <li>
          <BodyAddonName>Races/Raven/Wing/RavenWing_LayerB</BodyAddonName>
          <ReplacePath>Races/Raven/Special/ZuoYao/Wing/ZuoYao_Wing_LayerB</ReplacePath>
        </li>
        <li>
          <BodyAddonName>Races/Raven/Hair/RavenHair_A</BodyAddonName>
          <ReplacePath>Races/Raven/Special/ZuoYao/Hair/ZuoYao_Hair</ReplacePath>
        </li>
        <li>
          <BodyAddonName>Races/Raven/Hair/RavenHair_A_Back</BodyAddonName>
          <ReplacePath>Races/Raven/Special/ZuoYao/Hair/ZuoYao_Hair_Back</ReplacePath>
        </li>
      </bodyAddonRules>

      <!-- 服装材质替换 -->
      <ApparelStuffRules>
        <li>
          <Apparel>Raven_Apparel_Qipao</Apparel>
          <ReplaceStuff>DevilstrandCloth</ReplaceStuff>
        </li>
      </ApparelStuffRules>

      <!-- 渲染节点规则 -->
      <renderNodeRules>
        <li>
          <tagDefName>Head</tagDefName>
          <replacePath>Races/Raven/Special/ZuoYao/Head/ZuoYao_Head</replacePath>
        </li>
        <li>
          <tagDefName>Hair</tagDefName>
          <replacePath>Races/Raven/Special/ZuoYao/Hair/ZuoYao_Hair</replacePath>
          <fixedColor>(0.85, 0.15, 0.15, 1)</fixedColor>
        </li>
      </renderNodeRules>
    </li>
  </modExtensions>
</PawnKindDef>
```

---

## 注意事项

1. **颜色格式**：Color 使用 RGBA 浮点值 `(R, G, B, A)`，范围 0~1。`(0,0,0,0)` 即 `Color.clear`，表示"不替换"。
2. **Vector3 格式**：偏移使用 `(X, Y, Z)` 格式，Y 轴为上下方向。
3. **路径规则**：贴图路径不含 `Textures/` 前缀和文件扩展名，与 RimWorld 贴图路径规范一致。
4. **匹配优先级**：`renderNodeRules` 中 `tagDefName` 优先于 `debugLabel` 进行匹配。
5. **bodyAddonRules vs renderNodeRules**：前者用于 HAR 的 BodyAddon，后者用于原生渲染节点（Head/Body/Hair 等）。两者作用目标不同，不要混用。
