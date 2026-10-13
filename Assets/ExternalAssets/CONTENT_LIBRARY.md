# Dead Sector — фотореалистичная тестовая библиотека ассетов

**Ветка:** `test/assets-library`. **Локальная тестовая копия:** `Dead Sector_Test`.
**Не изменять** `main` или оригинальную папку `Dead Sector`. Дата переработки требований: 2026-10-13.

## СТРОГОЕ ПРАВИЛО СТИЛЯ

Dead Sector — реалистичная постапокалиптика: ориентиры по визуальному качеству и атмосфере S.T.A.L.K.E.R. 2 и DayZ. Критерии: реальные пропорции, PBR-материалы, фотограмметрия / правдоподобные формы, следы износа, характерная постсоветская индустриальная архитектура, анатомически убедительные герои и зомби.

**Стилизованные/low-poly мультяшные пакеты Kenney, Quaternius не использовать даже для тестовой карты.** Предыдущие рекомендации таких ассетов отозваны и удалены из `ASSET_DOWNLOAD_MANIFEST.csv`. Low-poly оптимизированная сетка допустима **только при сохранении реалистичного визуального стиля**.

Этот коммит содержит реестр/папки, **не импортирует** новые FBX/GLB/Blend/ZIP модели или текстуры.

## Что уже есть

- Реально присутствуют в ветке GitHub: `Terrain/Textures/ForestGround03`, `Terrain/Materials/DS_ForestGround03.terrainlayer`, Unity TerrainCatalog ссылка. Не дублировать.
- Ранее пользователь скачал 13 архивов Poly Haven: `fir_tree_01`, `pine_sapling_medium`, `pine_sapling_small`, `shrub_sorrel_01`, `shrub_03`, `nettle_plant`, `dandelion_01`, `weed_plant_02`, `grass_medium_01`, `grass_medium_02`, `grass_bermuda_01`, `tree_stump_01`, `rock_moss_set_02`. **Не подтверждено**, что эти архивы импортированы и находятся в GitHub.
- Локальная кукуруза `Maize Corn Plant` уже проходила подготовку с лицензией CC BY 4.0; см. `ASSET_SOURCES.md`. Эти файлы в тестовой GitHub-ветке не присутствуют.

## Проверенные страницы загрузок — природа / индустрия / декорации (Poly Haven, CC0)

- [Dead Tree Trunk 02](https://polyhaven.com/a/dead_tree_trunk_02) — сухой ствол, 156K исходных трис, оптимизировать LOD.
- [Boulder 01](https://polyhaven.com/a/boulder_01) — крупный камень.
- [Modular Factory Facade](https://polyhaven.com/a/modular_factory_facade) — **175K трис на полный набор модулей**, PBR индустриальный фасад завода. Предпочесть 2K при импорте, не весь 8K пакет.
- [Modular Urban Apartments Facade](https://polyhaven.com/a/modular_urban_apartments_facade) — жилые фасады, не содержит автоматически готовых игровых интерьеров.
- [Modular Industrial Pipes 01](https://polyhaven.com/a/modular_industrial_pipes_01) — трубы.
- [Modular Airduct Circular 01](https://polyhaven.com/a/modular_airduct_circular_01) — вентиляция.
- [Portable Generator](https://polyhaven.com/a/portable_generator) — генератор.
- [Steel Frame Shelves 01](https://polyhaven.com/a/steel_frame_shelves_01) — стеллажи.
- [Wooden Crate 01](https://polyhaven.com/a/wooden_crate_01) — ящик.
- [Concrete Road Barrier](https://polyhaven.com/a/concrete_road_barrier) — заграждение.
- [Covered Car](https://polyhaven.com/a/covered_car) — накрытый старый автомобиль.
- [Old Tyre](https://polyhaven.com/a/old_tyre) — старая шина.
- [Rusted Wheel Rim 01](https://polyhaven.com/a/rusted_wheel_rim_01) — ржавый диск колеса.
- [Crowbar 01](https://polyhaven.com/a/crowbar_01) и [Wooden Axe 02](https://polyhaven.com/a/wooden_axe_02) — игровой реквизит оружия ближнего боя.

Лицензия Poly Haven: https://polyhaven.com/license ; CC0 разрешает коммерческий релиз и исходные ассеты в публичном GitHub (без претензий на авторство).

## Проверенные страницы загрузок — реалистичный постсоветский сеттинг (Sketchfab, CC BY)

Ссылки проверены как страницы моделей с кнопкой **Download 3D Model** и лицензией **CC Attribution**. При публикации игры сохранять имя автора, URL модели, лицензию и описание переработки; сырые файлы не пушить до оформления реестра лицензий и подтверждения условий.

- [Abandoned Soviet Garages Pack Game-Ready](https://sketchfab.com/3d-models/abandoned-soviet-garages-pack-game-ready-fcf453872c2d4cfbad12197d072fbfc4), автор `olegolegperepelica` — 4 гаража, PBR 4K, 9.8K tris.
- [Abandoned Soviet Gas Station](https://sketchfab.com/3d-models/abandoned-soviet-gas-station-76f8b45993f14303a5071e456b668290), автор `olegolegperepelica` — 12.9K tris.
- [Abandoned Building Modular Environment Kit](https://sketchfab.com/3d-models/abandoned-building-modular-environment-kit-376aead6cd3d41bda715cbab99c440fa), автор `olegolegperepelica` — модульные внутренние помещения.
- [Abandoned Soviet Generator](https://sketchfab.com/3d-models/abandoned-soviet-generator-game-ready-ue5-prop-0f11d8e9b59b431295e42ddf579c38ce), автор `olegolegperepelica` — 15.1K tris, 4K текстуры (снизить до 2K).
- [Abandoned Soviet BTR-80](https://sketchfab.com/3d-models/abandoned-soviet-btr-80-32145d6303e5487e9d92097b9845ef02), автор `Coozy` — 107.7K tris, оптимизировать.
- [Abandoned Soviet Car IZH-2715](https://sketchfab.com/3d-models/abandoned-soviet-car-izh-2715-b97884f0730d42a68c1a6a9a9874159d), автор `Mikhail Volkov` — скан 200K tris; требуется ретопология.
- [Zombie Character](https://sketchfab.com/3d-models/zombie-character-5535cecb720f4682bd29a591f745cd6e), автор `Harri Jokinen` — 32.2K tris; **кандидат**, ещё надо визуально проверить реалистичность, скелет и анимацию.
- [Zombie Soldier](https://sketchfab.com/3d-models/zombie-soldier-dc349067a1fa466680ecce844caab06d), автор `Am I dead?` — визуально реалистичный, но 376.8K tris: **только после ретопологии**, не для толпы в исходном виде.

## Реалистичные PBR текстуры

Источник https://ambientcg.com (CC0): асфальт, мокрая грязь, бетон, облезлая штукатурка, кирпич, ржавчина. Выбирать конкретные карты на сайте, вносить ID и ссылку в `ASSET_SOURCES.md` перед импортом. Сейчас `ASSET_DOWNLOAD_MANIFEST.csv` помечает сайт `SOURCE_LIBRARY`, а не якобы уже скачанный пакет.

## Структура директорий

`Assets/ExternalAssets/Vegetation/` — модели травы, деревьев, кустарников, камней, текстуры и LOD.
`Assets/ExternalAssets/Terrain/` — карты земли, асфальта, грязи, бетона, прежний ForestGround03.
`Assets/ExternalAssets/Buildings/` — жилые, индустриальные, руины, модульные здания.
`Assets/ExternalAssets/Props/` — интерьеры, заводские объекты, лут, уличные объекты.
`Assets/ExternalAssets/Vehicles/` — машины и обломки.
`Assets/ExternalAssets/Characters/` — игрок/зомби/анимации.
`Assets/ExternalAssets/Weapons/` — оружие.
`Assets/ExternalAssets/Farming/` — растения и еда.

## Технические правила импорта в Unity 6000.6.4f1 / URP 17.7.0

1. Прежде чем переключать локальную `Dead Sector_Test` ветку, проверить `git status` и сохранить изменения. `main` и оригинальный проект не трогать.
2. Для моделей брать FBX/glTF и PBR-карты. Архивы хранить **вне папки Unity Assets**; исходные Blender файлы не загружать массово без оптимизации.
3. Трава/кусты — 1K, крупные ближние пропсы и здания — 2K; Hero-объекты 4K только если доказана необходимость.
4. URP/Lit: Base Color, Normal map (OpenGL нормаль), Metallic/Smoothness, Alpha Clip для листьев.
5. LODGroup для растительности, отдельные простые коллайдеры, Terrain detail / GPU instancing, culling и стриминг 8×8км. Скан из 100–400K трис нельзя массово размножать в игровом мире.
6. Начать с участка 100×100м и визуального теста: 10 деревьев, трава, 5 кустов, 3 камня, 1 реалистичный гараж. Затем — растительность + завод и сравнение FPS/Draw Calls на Radeon 680M.
7. Перед публичным пушем стороннего ассета записать автора, ссылку, лицензию, текст атрибуции и преобразования; Unity `.meta` сохранять вместе с импортом.
8. Сырые файлы Mixamo не публиковать в публичном репозитории. https://helpx.adobe.com/creative-cloud/faq/mixamo-faq.html

**Что ещё нужно подобрать в реализме:** мужская и женская модели игроков с Humanoid rig, гражданские/рабочие/военные зомби, полноценные интерьеры домов, продукты, рюкзаки, броня, три слота оружия. Эти позиции пока **не закрыты**: не объявлять их уже найденными/скачанными.

Этот документ является планом загрузки; сами новые модели в тестовый Unity проект **не импортированы**.
