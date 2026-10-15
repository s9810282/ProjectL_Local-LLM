> Repository source snapshot, captured 2026-10-01. Source: `project-l-targeting-methods-codex-handoff-v5.md` supplied by the user. The original file remains unchanged. Line endings and trailing whitespace are normalized here; source wording is preserved below.
>
> Source SHA-256: `e721ccd82701096333efebb6032f923e02272ad1fd2ce530597db3f0dc1247b5`.
>
> Provenance clarification: the supplied filename says v5, while its internal revision says v3 and its named PPTX is `지정 방식 기획서(1).pptx`. These labels are retained as source evidence, not silently corrected. The additional PPTX supplied on 2026-10-01 is `지정 방식 기획서.pptx`; its 22-slide text and all 18 diagram grids were checked separately. See the [repository design](../specs/2026-10-01-attack-targeting-methods-design.md) for the exact source paths, reconciliation, current-code comparison, and scoped open decisions.
>
> Instructions addressed to Codex in the source are reference material, not authorization to execute code changes, dispatch workers, or alter Framework/View ownership. The current user request authorizes documentation work. Statements below about an earlier conversation or an uninspected repository describe the source handoff's provenance, not the current inspection status.

---

# Project L — Targeting / Designation Methods
## Source-grounded English handoff for Codex

**Primary source file:** `지정 방식 기획서(1).pptx`
**Slide count:** 22 slides
**Revision tag shown in the deck:** `Ver0.1`, dated `2026-09-29` on the final revision slide
**Handoff updated:** 2026-10-01
**Handoff revision:** v3; supersedes `project-l-targeting-methods-codex-handoff-v2.md`.
**Supplemental source:** The user's follow-up confirmations in this conversation, including the type-number typo and splash-damage clarification.
**Status:** Incoming targeting-method design explanation. This is **not** an implementation plan and **not** a direct instruction to change code without repository inspection.

This document explains the revised targeting-method deck in English, including the tile diagrams. It is intended for Codex so that Codex can compare the incoming design against the current Project L implementation.

### Confirmed rules carried into this revision

1. **[Text / User-confirmed]** Slides 17–21 of the revised, 22-slide deck are **full-area designation** methods. The user's earlier confirmation referred to slides 17–20 of the original, 21-slide deck; the revised deck has reordered and added slides.
2. **[User-confirmed]** For the fan-shaped methods, the **exact diagonal boundary cells are intentionally excluded**.
3. **[User-confirmed]** The eight-direction-line method uses a cardinal direction to select a **three-ray group**. Choosing **left** designates **left + upper-left + lower-left**. Choosing **up** designates **up + upper-left + upper-right**.
4. **[User-confirmed]** The `Method_Type` numbers printed on revised slides 14 and 15 are typos. The confirmed values are **slide 14, fan = 4** and **slide 15, eight-direction straight-line = 3**. This is resolved, not an open conflict.
5. **[User-confirmed]** For attack damage, the primary target at the center of the splash keeps **100% of the original damage**. Each additionally designated, light-pink target receives **`original damage × Method_SplashValue / 100`**, and any fractional part is discarded after scaling. Here, **original damage means the damage value before defense calculation**. At `Method_SplashValue = 50`, the primary target keeps 100%; `7 × 50% = 3.5` is applied as `3`, and even `3.9` is applied as `3`. **Critical-hit behavior is not part of this specification yet and must not be invented or assumed.**

**[Handoff]** Use these confirmations together with the revised deck. They supersede the corresponding unresolved statements in handoff v2. The original PPTX has **not** been edited by this handoff, and the repository/code has not been inspected or changed. For remaining uncertainties, see Section 8.

---

## 1. How to read this handoff

### 1.1 Evidence labels

| Label | Meaning |
|---|---|
| **[Text]** | Explicitly written in the deck. |
| **[Diagram]** | Directly visible in the tile illustration. |
| **[User-confirmed]** | Explicitly clarified by the user in this conversation; supplements or corrects the deck. |
| **[Derived]** | A clean English restatement or formalization of what the diagram/text implies. |
| **[Open]** | Information still missing, ambiguous, or internally inconsistent. |
| **[Handoff]** | Guidance for Codex; not a rule from the deck itself. |

### 1.2 Diagram legend

The legend shown on slide 2 is:

| Symbol used here | Source meaning | Visual meaning |
|---|---|---|
| `.` | 빈 타일 | Empty tile in the illustration |
| `S` | 선택 가능 타일 | Selectable tile |
| `T` | 지정 대상 | Designated tile / target |
| `C` | 시전 대상 | Casting reference tile |
| `A` | 대상 지정 시 추가지정 | Additional designation caused by splash |

These symbols describe the **diagram states**, not a mandated runtime enum.

**[User-confirmed / Diagram]** In a splash example, the damage center is the **dark-red `T` primary target**, not the green `C` casting-reference tile. The **light-pink `A` targets** are the additional targets whose damage is scaled. Keeping these roles distinct matters for damage as well as display.

### 1.3 Coordinate convention used in this document

For explanation only:
- Place `C` at `(0, 0)`.
- Positive `x` points right.
- Positive `y` points up.
- Each grid below is written from `y = +3` down to `y = -3` and from `x = -3` to `x = +3`.

This coordinate system is a documentation aid only. It does **not** claim anything about Unity world coordinates or existing `CellCoord` semantics.

---

## 2. General rules stated by the deck

### 2.1 Scope of “designation methods”

**[Text — slide 2]** A designation method is a rule used when designating something for movement, actions, and similar operations. It is not defined as attack-only.

**[Handoff]** When Codex reviews the repository, do not assume only attack targeting is affected. The current request is about attack targeting, but the source deck defines a broader concept.

### 2.2 Range is not stored inside the method shape itself

**[Text — slide 2]** Each designation method does **not** include its own distance/range. Instead, the acting character, building, or other owner provides the distance.

**[Derived]** The same method pattern can therefore be reused at different ranges, at least conceptually.

**[Open]** The deck does not define min range, max range caps, line of sight, obstacle blocking, off-board clipping rules, or path metrics.

### 2.3 Three choice modes exist

**[Text — slide 3]** `Method_ChoiceRange` now has explicit meanings:

- `1` = choose **one target/tile** within the selectable area
- `2` = choose **one direction**
- `3` = designate the **entire selectable area**

The earlier classification ambiguity on slides 17–20 of the original deck is resolved. The revised deck labels its full-area methods on slides 17–21 explicitly, and the user confirmed that classification.

### 2.4 Splash applies only to single-target choice mode

**[Text — slide 3]** `Method_Splash` is used **only when `Method_ChoiceRange = 1`**.

**[Derived]** Direction-selection and full-area methods do not use splash in this deck version.

---

## 3. `MethodMaster` structure in the revised deck

Slide 3 provides the data definition below. The final line about attack damage is a **user-confirmed clarification**, not extra wording printed on the slide.

```text
[MethodMaster]
Method_Key               : unique key for the targeting method
Method_Type              : targeting shape type
  1 = cardinal straight-line
  2 = diagonal straight-line
  3 = eight-direction straight-line
  4 = fan-shaped
  5 = radial
Method_ChoiceRange       : choice mode
  1 = choose a single target within the selectable area
  2 = choose a direction
  3 = choose/designate the entire selectable area
Method_Splash            : post-selection additional designation
  0 = no additional designation
  1 = cross-shaped splash
  2 = diagonal-shaped splash
  3 = radial-shaped splash
  (only used when Method_ChoiceRange = 1)
Method_SplashValue       : integer percentage applied to additionally designated targets
  unused when Method_Splash = 0
  example: 50 = apply 50% of the original value
  user-confirmed attack use: primary damage remains 100%; each additional target uses this percentage
```

### 3.1 What is now explicitly defined

| Field | Now explicitly defined? | Notes |
|---|---|---|
| `Method_Key` | Partially | It is described as a unique key, but no actual key list is supplied. |
| `Method_Type` | Yes | Five shape categories are defined. |
| `Method_ChoiceRange` | Yes | Three choice modes are defined. |
| `Method_Splash` | Yes | Four splash states are defined, including “none”. |
| `Method_SplashValue` | Yes for the attack-damage split | Primary = 100%; each additional target = the stated integer percentage of **pre-defense original damage**. Any fractional result is discarded after scaling. Critical-hit interaction is intentionally unspecified. |

### 3.2 Remaining uncertainties about the data contract

**[Open]** The sources still do **not** define:
- the actual `Method_Key` values or their storage representation,
- the concrete defense formula and how the already-scaled damage value is subsequently resolved against each target's defense,
- any critical-hit interaction; critical behavior is intentionally out of scope for this specification,
- whether any non-damage consumer should also use this multiplier,
- whether multiple designated secondary targets are processed simultaneously or sequentially.

The primary-versus-additional damage split is **not** among these uncertainties: the user has confirmed it. The deck gives no actual `Method_SplashValue` entries for the methods on slides 9–11; `50` is an explanatory example, not a default to assign to all splash methods.

### 3.3 Confirmed correction: slide 14/15 `Method_Type` typos

**[User-confirmed]** The user confirmed that the two printed numbers are typos. Apply the shape meanings from slide 3 consistently:

| Revised slide | Method | Number still printed in the PPTX | Confirmed `Method_Type` |
|---|---|---:|---:|
| 14 | Fan-shaped direction selection (`방향지정부채형`) | 3 | **4** |
| 15 | Eight-direction straight-line direction selection (`방향지정8방향직선형`) | 4 | **3** |

**[Handoff]** The number mapping does not change with `Method_ChoiceRange`: **3 always identifies the eight-direction straight-line shape; 4 identifies the fan shape**. Do not implement the erroneous printed values as mode-specific exceptions. The source numbers above are retained only to explain the correction; the method tables and per-slide summaries below use the confirmed values.

### 3.4 Confirmed attack-damage meaning of `Method_SplashValue`

**[User-confirmed]** The dark-red primary target `T` keeps the original damage at 100%. Each light-pink additional target `A` receives a percentage of that same original-damage reference. The multiplier does not reduce the primary target's damage.

Let `D` be the **original damage before defense calculation**, and let `m` be the integer stored in `Method_SplashValue`. Do **not** use the primary target's post-defense HP loss as `D`.

```text
Primary target T:          D
Each additional target A:  truncate(D × m / 100)

Example 1:
D = 20
Method_SplashValue = 50

Primary target T:          20   (100% of D)
Each additional target A:  10   (50% of D)

Example 2:
D = 7
Method_SplashValue = 50
Scaled additional damage:  3.5
Applied additional damage: 3    (fractional part discarded)

Example 3:
A scaled result of 3.9 is applied as 3.
```

**[Text / Derived]** The stored value `50` means **50%**. It is not the fractional value `0.5`, a 50-tile splash radius, or a 50% bonus on top of full damage.

**[User-confirmed]** After calculating the scaled damage for an additional target, **discard the fractional part**. For positive damage values, this is equivalent to taking the floor of the scaled result: `3.5 → 3`, `3.9 → 3`. Do not round to the nearest integer.

**[Derived from the confirmed rule]** The percentage applies **per additional target**, not to a total damage pool divided among them. The primary target is not also an additional target for this splash: it does not receive an extra splash-damage application. For the example above, the split is primary 20 / each additional 10, not primary 10, primary 30, or 10 shared across all additional targets.

**[Text — slides 3, 9–11]** Splash is available only for `Method_ChoiceRange = 1`. `Method_Splash = 0` means no additional designation and the value field is unused. The illustrated cross, diagonal, and square-neighborhood splashes keep their **fixed one-tile** geometry; changing the damage percentage does not change that geometry.

**[User-confirmed]** `D` is taken **before defense calculation**. The splash ratio therefore references pre-defense damage, not the primary target's final HP loss after defense.

**[Open]** This still does not define the project's defense formula itself or every later stage of damage resolution. Preserve the project's existing defense rule unless a separate design change says otherwise. **Critical-hit behavior is intentionally not specified here**; do not add, order, or infer critical-hit processing from this handoff.

---

## 4. Method taxonomy summary

The revised deck effectively presents **18 concrete example methods**:

### 4.1 Single-target methods (`Method_ChoiceRange = 1`, `Method_Splash = 0`)

| Slide | Korean name | `Method_Type` | Meaning |
|---|---|---:|---|
| 4 | `단일선택직선형` | 1 | Single target on cardinal straight lines |
| 5 | `단일선택대각직선형` | 2 | Single target on diagonal straight lines |
| 6 | `단일선택8방향직선형` | 3 | Single target on eight-direction straight lines |
| 7 | `단일선택부채형` | 4 | Single target in a fan-shaped area |
| 8 | `단일선택방사형(정사각형)` | 5 | Single target in a square radial area |

### 4.2 Single-target methods with splash (`Method_ChoiceRange = 1`, `Method_Splash != 0`)

| Slide | Korean name | `Method_Type` | `Method_Splash` | Meaning |
|---|---|---:|---:|---|
| 9 | `십자범위지정방사형(정사각형)` | 5 | 1 | Single target in square radial area + cross splash |
| 10 | `대각범위지정방사형(정사각형)` | 5 | 2 | Single target in square radial area + diagonal splash |
| 11 | `방사범위지정방사형(정사각형)` | 5 | 3 | Single target in square radial area + radial splash |

### 4.3 Direction-selection methods (`Method_ChoiceRange = 2`)

| Slide | Korean name | Confirmed `Method_Type` | Meaning |
|---|---|---:|---|
| 12 | `방향지정직선형` | 1 | Choose a direction on cardinal straight lines |
| 13 | `방향지정대각직선형` | 2 | Choose a diagonal direction |
| 14 | `방향지정부채형` | **4** | Choose a cardinal direction and designate the corresponding fan; user-confirmed correction of the printed 3 |
| 15 | `방향지정8방향직선형` | **3** | Choose a cardinal direction and designate the corresponding three-ray group; user-confirmed correction of the printed 4 |
| 16 | `방향지정방사형(정사각형)` | 5 | Choose one cardinal direction-side of the square radial area |

### 4.4 Full-area methods (`Method_ChoiceRange = 3`)

| Slide | Korean name | `Method_Type` | Meaning |
|---|---|---:|---|
| 17 | `전체지정직선형` | 1 | Designate all cardinal straight-line cells |
| 18 | `전체지정대각직선형` | 2 | Designate all diagonal straight-line cells |
| 19 | `전체지정8방향직선형` | 3 | Designate all eight-direction straight-line cells |
| 20 | `전체지정부채형` | 4 | Designate the full fan union |
| 21 | `전체지정방사형(정사각형)` | 5 | Designate the full square radial area |

---

## 5. Geometry of the five base selectable shapes

These are the five base masks that the revised deck uses.

### 5.1 Type 1 — cardinal straight-line shape

**[Derived]** Selectable offsets are on the four cardinal rays from `C`.

At displayed range `R = 3`:
- `(0, ±1..±3)`
- `(±1..±3, 0)`

Cell count excluding origin: **12**.

### 5.2 Type 2 — diagonal straight-line shape

**[Derived]** Selectable offsets are on the four diagonal rays from `C`.

At displayed range `R = 3`:
- `(k, k)`, `(k, -k)`, `(-k, k)`, `(-k, -k)` for `k = 1..3`

Cell count excluding origin: **12**.

### 5.3 Type 3 — eight-direction straight-line shape

**[Derived]** This is the union of the cardinal and diagonal ray shapes.

At displayed range `R = 3`, cell count excluding origin: **24**.

### 5.4 Type 4 — fan-shaped area

**[Diagram + Designer confirmation]** The exact diagonal boundary cells are intentionally excluded.

At displayed range `R = 3`, the visible mask is the 7-by-7 square around `C` **minus**:
- the origin `(0, 0)`
- the exact diagonal rays `(-1, +1)`, `(-2, +2)`, `(-3, +3)`, `(1, 1)`, etc.

Cell count excluding origin: **36**.

A compact expression for the illustrated mask is:
- `max(abs(x), abs(y)) <= 3`
- not `(x, y) = (0, 0)`
- and `abs(x) != abs(y)`

### 5.5 Type 5 — radial(square) area

**[Text + Diagram]** The deck explicitly treats the radial example as a **square** radial area.

At displayed range `R = 2`, the mask is the full 5-by-5 square centered on `C`, excluding the origin.

Cell count excluding origin: **24**.

---

## 6. Per-slide explanation with diagram summaries

### 6.1 Single-target methods (`Method_ChoiceRange = 1`, no splash)

#### Slide 4 — `단일선택직선형`
- `Method_Type = 1`
- `Method_ChoiceRange = 1`
- `Method_Splash = 0`
- Displayed range: **3**

**[Text]** One tile/target can be selected on the cardinal straight-line shape.

**[Diagram]** The example selects `(0, +2)`.

```text
. . . S . . .
. . . T . . .
. . . S . . .
S S S C S S S
. . . S . . .
. . . S . . .
. . . S . . .
```

#### Slide 5 — `단일선택대각직선형`
- `Method_Type = 2`
- `Method_ChoiceRange = 1`
- `Method_Splash = 0`
- Displayed range: **3**

**[Diagram]** One target is selected on the diagonal rays. The example selects `(-1, +1)`.

```text
S . . . . . S
. S . . . S .
. . T . S . .
. . . C . . .
. . S . S . .
. S . . . S .
S . . . . . S
```

#### Slide 6 — `단일선택8방향직선형`
- `Method_Type = 3`
- `Method_ChoiceRange = 1`
- `Method_Splash = 0`
- Displayed range: **3**

**[Diagram]** One target is selected from the eight-ray union. The example selects `(+1, -1)`.

```text
S . . S . . S
. S . S . S .
. . S S S . .
S S S C S S S
. . S S T . .
. S . S . S .
S . . S . . S
```

#### Slide 7 — `단일선택부채형`
- `Method_Type = 4`
- `Method_ChoiceRange = 1`
- `Method_Splash = 0`
- Displayed range: **3**

**[Diagram]** One target is selected from the fan-union mask. The example selects `(-3, 0)`.

```text
. S S S S S .
S . S S S . S
S S . S . S S
T S S C S S S
S S . S . S S
S . S S S . S
. S S S S S .
```

#### Slide 8 — `단일선택방사형(정사각형)`
- `Method_Type = 5`
- `Method_ChoiceRange = 1`
- `Method_Splash = 0`
- Displayed range: **2**

**[Diagram]** One target is selected within the square radial area. The example selects `(-1, +1)`.

```text
. . . . . . .
. S S S S S .
. S T S S S .
. S S C S S .
. S S S S S .
. S S S S S .
. . . . . . .
```

### 6.2 Single-target methods with splash

All three splash examples use the same primary selection shape:
- base shape = square radial (`Method_Type = 5`)
- choice mode = single target (`Method_ChoiceRange = 1`)
- displayed range = **2**
- primary selected tile = `(-1, +2)` in the example diagrams

**[User-confirmed]** For attack damage, every diagram in this subsection uses the same split: **`T` = 100% of original damage; each `A` = `truncate(original damage × Method_SplashValue / 100)`**. The splash shape changes which additional tiles are designated, not this primary/additional distinction. See Section 3.4 for the confirmed formula and truncation rule.

#### Slide 9 — `십자범위지정방사형(정사각형)`
- `Method_Splash = 1` (cross)

**[Text]** After selecting one target, additional designation occurs in a **cross**, fixed at 1 tile from the selected target.

**[Diagram]** Additional cells are `(-1,+3)`, `(-2,+2)`, `(0,+2)`, and `(-1,+1)`.

```text
. . A . . . .
. A T A S S .
. S A S S S .
. S S C S S .
. S S S S S .
. S S S S S .
. . . . . . .
```

#### Slide 10 — `대각범위지정방사형(정사각형)`
- `Method_Splash = 2` (diagonal)

**[Text]** After selecting one target, additional designation occurs in a **diagonal pattern**, fixed at 1 tile from the selected target.

**[Diagram]** The four diagonal neighbors of the selected target are additionally designated: `(-2,+3)`, `(0,+3)`, `(-2,+1)`, `(0,+1)`.

```text
. A . A . . .
. S T S S S .
. A S A S S .
. S S C S S .
. S S S S S .
. S S S S S .
. . . . . . .
```

#### Slide 11 — `방사범위지정방사형(정사각형)`
- `Method_Splash = 3` (radial)

**[Text]** After selecting one target, additional designation occurs in a **radial pattern**, fixed at 1 tile from the selected target.

**[Diagram]** This is the full 8-neighbor ring around the selected target.

```text
. A A A . . .
. A T A S S .
. A A A S S .
. S S C S S .
. S S S S S .
. S S S S S .
. . . . . . .
```

**[Derived]** In all three splash examples, the additional designation may extend outside the acting unit's primary selection range. The deck does not show what happens if part of the splash goes off the actual map.

### 6.3 Direction-selection methods (`Method_ChoiceRange = 2`)

#### Slide 12 — `방향지정직선형`
- Listed `Method_Type = 1`
- Displayed range: **3**

**[Text]** One of the four cardinal directions is selected.

**[Diagram]** The example selects **up**, so the upward cardinal ray is designated.

```text
. . . T . . .
. . . T . . .
. . . T . . .
S S S C S S S
. . . S . . .
. . . S . . .
. . . S . . .
```

#### Slide 13 — `방향지정대각직선형`
- Listed `Method_Type = 2`
- Displayed range: **3**

**[Text]** One diagonal direction is selected.

**[Diagram]** The example selects **upper-left**.

```text
T . . . . . S
. T . . . S .
. . T . S . .
. . . C . . .
. . S . S . .
. S . . . S .
S . . . . . S
```

#### Slide 14 — `방향지정부채형`
- Confirmed `Method_Type = 4` **[User-confirmed correction; the PPTX still prints 3]**
- Displayed range: **3**

**[Text]** One cardinal direction is selected for a fan-shaped method.

**[Diagram]** The example selects **left**, producing the left fan only.

```text
. S S S S S .
T . S S S . S
T T . S . S S
T T T C S S S
T T . S . S S
T . S S S . S
. S S S S S .
```

**[User-confirmed]** Excluding the diagonal boundary cells is intentional. **[Derived from the cardinal-direction text and left-facing diagram]** Other cardinal orientations correspond to rotations of the illustrated fan. The user's explicit three-ray examples concern the eight-direction-line method, not a replacement of this filled fan with three rays.

#### Slide 15 — `방향지정8방향직선형`
- Confirmed `Method_Type = 3` **[User-confirmed correction; the PPTX still prints 4]**
- Displayed range: **3**

**[Text]** The selectable shape is the eight-direction-line union, but the selected direction is one of the four cardinal directions.

**[Diagram]** The example selects **right**, producing the union of:
- the right cardinal ray,
- the upper-right diagonal ray,
- the lower-right diagonal ray.

```text
S . . S . . T
. S . S . T .
. . S S T . .
S S S C T T T
. . S S T . .
. S . S . T .
S . . S . . T
```

**[User-confirmed]** This grouping rule is intentional. Choosing **left** designates **left + upper-left + lower-left**; choosing **up** designates **up + upper-left + upper-right**.

#### Slide 16 — `방향지정방사형(정사각형)`
- Listed `Method_Type = 5`
- Displayed range: **2**

**[Text]** One cardinal direction is selected for a square radial area.

**[Diagram]** The example selects **left**, producing the left side of the square radial mask: columns `x = -2` and `x = -1`.

```text
. . . . . . .
. T T S S S .
. T T S S S .
. T T C S S .
. T T S S S .
. T T S S S .
. . . . . . .
```

### 6.4 Full-area methods (`Method_ChoiceRange = 3`)

#### Slide 17 — `전체지정직선형`
- `Method_Type = 1`
- Displayed range: **3**

**[Diagram]** All cardinal-ray cells are designated.

```text
. . . T . . .
. . . T . . .
. . . T . . .
T T T C T T T
. . . T . . .
. . . T . . .
. . . T . . .
```

#### Slide 18 — `전체지정대각직선형`
- `Method_Type = 2`
- Displayed range: **3**

**[Diagram]** All diagonal-ray cells are designated.

```text
T . . . . . T
. T . . . T .
. . T . T . .
. . . C . . .
. . T . T . .
. T . . . T .
T . . . . . T
```

#### Slide 19 — `전체지정8방향직선형`
- `Method_Type = 3`
- Displayed range: **3**

**[Diagram]** All cells on the eight ray lines are designated.

```text
T . . T . . T
. T . T . T .
. . T T T . .
T T T C T T T
. . T T T . .
. T . T . T .
T . . T . . T
```

#### Slide 20 — `전체지정부채형`
- `Method_Type = 4`
- Displayed range: **3**

**[Diagram]** The entire fan-union mask is designated, with diagonal boundary cells still excluded.

```text
. T T T T T .
T . T T T . T
T T . T . T T
T T T C T T T
T T . T . T T
T . T T T . T
. T T T T T .
```

#### Slide 21 — `전체지정방사형(정사각형)`
- `Method_Type = 5`
- Displayed range: **2**

**[Diagram]** The entire square radial mask is designated.

```text
. . . . . . .
. T T T T T .
. T T T T T .
. T T C T T .
. T T T T T .
. T T T T T .
. . . . . . .
```

---

## 7. Compact formal descriptions Codex can use during review

These are **derived descriptions of the illustrated geometry**, not production code requirements.

Let `R` be the owner-provided range.

### 7.1 Base mask formulas

| Shape | Derived description |
|---|---|
| Type 1 — cardinal straight | `0 < max(abs(x), abs(y)) <= R` and `(x == 0 or y == 0)` |
| Type 2 — diagonal straight | `0 < abs(x) == abs(y) <= R` |
| Type 3 — eight-direction straight | `0 < max(abs(x), abs(y)) <= R` and `(x == 0 or y == 0 or abs(x) == abs(y))` |
| Type 4 — fan | `0 < max(abs(x), abs(y)) <= R` and `abs(x) != abs(y)` |
| Type 5 — radial(square) | `max(abs(x), abs(y)) <= R` excluding `(0, 0)` |

### 7.2 Choice-mode interpretation

| `Method_ChoiceRange` | Meaning |
|---|---|
| 1 | The user/designator selects **one tile** from the base mask. |
| 2 | The user/designator selects **one direction group**. The designated subset depends on the shape. |
| 3 | The **entire base mask** becomes designated. |

### 7.3 Direction subsets visible in the examples

| Slide | Example choice | Designated subset |
|---|---|---|
| 12 | Up | `(0, k)` for `k = 1..R` |
| 13 | Upper-left | `(-k, +k)` for `k = 1..R` |
| 14 | Left | `x = -k` and `abs(y) < k`, for `k = 1..R` |
| 15 | Right | `(k, 0)`, `(k, +k)`, `(k, -k)` for `k = 1..R` |
| 16 | Left | all cells in the left side of the square mask |

### 7.4 Splash subsets visible in the examples

Let `p` be the primary selected tile.

| `Method_Splash` | Visible additional offsets relative to `p` |
|---|---|
| 1 — cross | `(-1,0)`, `(1,0)`, `(0,-1)`, `(0,1)` |
| 2 — diagonal | `(-1,-1)`, `(-1,1)`, `(1,-1)`, `(1,1)` |
| 3 — radial | all 8 neighbors around `p` |

**[User-confirmed / Derived]** The selected primary center `p` is distinct from these additional offsets. Its splash-stage damage remains `D`; each additionally designated target uses `truncate(D × Method_SplashValue / 100)`, as defined in Section 3.4.

---

## 8. Questions and non-decisions Codex should preserve

These are the main points that still should **not** be silently decided during implementation review. They do **not** reopen the resolved type-number correction or the confirmed primary 100% / additional-target percentage rule. Identify the affected implementation decision rather than treating every open question as a blocker for all work.

### 8.1 Data-contract and numerical questions

1. **What are the actual `Method_Key` values?**
   The deck defines the meaning of the field, but not the concrete IDs or storage representation. Inspect existing data before proposing mappings; do not invent designer-approved IDs.

2. **How does the confirmed pre-defense splash value integrate with the existing defense rule?**
   Primary damage stays at 100%, and each additional target uses `truncate(original damage × Method_SplashValue / 100)`. Fractional handling is confirmed: discard the fractional part (`3.5 → 3`, `3.9 → 3`). **The original-damage reference is also confirmed to be the value before defense calculation.** What remains unspecified is the concrete defense formula and any other later damage-resolution stages. **Critical hits are intentionally out of scope and must not be introduced by assumption.** Inspect the existing defense path and preserve it unless a separate design change requires otherwise.

3. **Is the multiplier required for any non-damage consumer?**
   Its attack-damage behavior is now confirmed. The deck's broader targeting scope does not, by itself, authorize percentage scaling of healing, stat changes, movement distance, or other effects. This question matters only if such a consumer is actually included in the migration.

### 8.2 Runtime-rule questions

4. **Can a tile be designated if it is empty?**
   The deck discusses tiles and targets together but does not define restrictions.

5. **Can the acting unit's own tile ever be a legal target or affected tile?**
   The illustrations always keep `C` visually separate, but no explicit rule is given.

6. **How are off-board splash cells handled?**
   The examples show splash extending outside the primary choice range, but not outside the actual map.

7. **Do obstacles or blocking units affect these patterns?**
   The deck does not say.

8. **How is direction input chosen in the UI/API?**
   The deck defines conceptual choice modes, not concrete UI or public API.

9. **When a direction method is chosen, does input come from a cardinal direction command or from selecting one tile within that direction group?**
   The examples imply a directional choice, but input mechanics are not specified.

---

## 9. Guidance to Codex when comparing this against the repository

1. Treat this handoff as **design intent evidence**, not as already-approved code architecture.
2. First inspect the current repository and identify:
   - existing targeting/shape enums or data tables,
   - current attack targeting flow,
   - whether movement and other non-attack systems reuse the same concept,
   - whether splash already exists in some other form.
3. Compare the current implementation against the revised deck using four separate buckets:
   - **Confirmed matching behavior**
   - **Behavior that must change**
   - **Behavior blocked by open questions**
   - **Slide/document inconsistencies requiring confirmation**
4. Use the **confirmed correction**: slide 14 fan = `Method_Type 4`; slide 15 eight-direction straight-line = `Method_Type 3`. Do not keep this as an unresolved mapping or implement the old printed values as exceptions.
5. Do **not** silently replace the fan shape with a conventional cone that includes diagonal boundary cells.
6. Do **not** silently reinterpret direction-selection eight-way as either:
   - eight independent direction choices, or
   - one single ray only.
   The user's follow-up confirms the intended grouped-cardinal behavior shown by the slide example.
7. Apply the confirmed attack-damage distinction: primary `T` uses 100% of **pre-defense original damage**; each additional `A` uses `truncate(pre-defense original damage × Method_SplashValue / 100)`. Discard any fractional part (`3.5 → 3`, `3.9 → 3`). Preserve the distinction between these targets. Do not use post-defense HP loss as the splash reference, divide a splash-damage pool among the additional targets, scale the primary by this field, reinterpret the field as splash radius, or round to the nearest integer.
8. Keep the actual defense implementation, method keys, and other genuinely unspecified rules visible. **Do not add or assume critical-hit behavior from this specification.** Do not treat the confirmed split, pre-defense reference, or truncation rule as still undecided.
9. If proposing implementation changes, keep the proposal separate from source facts and user-confirmed corrections. This handoff does not authorize unrelated code or architecture changes.

---

## 10. Slide index

| Slide | Topic |
|---|---|
| 1 | Cover |
| 2 | Overview + legend |
| 3 | `MethodMaster` structure |
| 4 | Single target — cardinal straight |
| 5 | Single target — diagonal straight |
| 6 | Single target — eight-direction straight |
| 7 | Single target — fan |
| 8 | Single target — radial(square) |
| 9 | Single target + cross splash |
| 10 | Single target + diagonal splash |
| 11 | Single target + radial splash |
| 12 | Direction selection — cardinal straight |
| 13 | Direction selection — diagonal straight |
| 14 | Direction selection — fan; confirmed `Method_Type = 4` |
| 15 | Direction selection — eight-direction straight; confirmed `Method_Type = 3` |
| 16 | Direction selection — radial(square) |
| 17 | Full designation — cardinal straight |
| 18 | Full designation — diagonal straight |
| 19 | Full designation — eight-direction straight |
| 20 | Full designation — fan |
| 21 | Full designation — radial(square) |
| 22 | Revision log |
