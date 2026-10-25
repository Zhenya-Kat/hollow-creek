# Unity CLI на этом ПК

Проект: `C:\Users\Zhenya\Documents\hollow-creek`.
Unity Editor: `6000.6.3f1`. В проекте установлен `com.unity.pipeline` версии `0.8.0-exp.1`.
CLI доступен через `unity`; полный путь: `C:\Users\Zhenya\AppData\Local\Unity\bin\unity.exe`.

## Подключение из Codex

Запускайте команды Unity CLI через `exec_command` с `sandbox_permissions: "require_escalated"` и `prefix_rule: ["unity", "command"]`. В обычной ограниченной среде Codex нет доступа к защищённому файлу `Library/Pipeline/.unity-pipeline-port`. Поэтому CLI может ошибочно сообщать `No Pipeline instance found` или `No Unity Editor instances found with reachable Pipeline servers`, даже когда из пользовательского терминала подключение работает.

Не считайте такую ошибку доказательством, что Unity закрыт. Повторите команду с расширенным доступом. Не выводите содержимое файла подключения: он содержит токен авторизации.

Примеры для PowerShell:

```powershell
unity command --project-path C:\Users\Zhenya\Documents\hollow-creek editor_status
unity command --project-path C:\Users\Zhenya\Documents\hollow-creek list_open_scenes
unity command --project-path C:\Users\Zhenya\Documents\hollow-creek
unity pipeline list
```

Последняя команда показывает обнаруженные проекты; её результат в ограниченной среде также может не показывать порт и доступность сервера. Unity должен быть открыт с этим проектом и работающим Pipeline. Проверяйте `editor_status` перед изменениями: состояние компиляции, перезагрузку домена и Play Mode. Изменяйте сохранённые сцены и prefab вне Play Mode; сохраняйте существующие пользовательские изменения.

## Выполнение C# и проверка изменений

Для операций с Unity API используйте `run_script`: сохраните C# файл в `Temp/` вне `Assets/`, затем выполните его статическую точку входа. Это компилирует файл в памяти без импорта нового скрипта в проект.

```powershell
unity command --project-path C:\Users\Zhenya\Documents\hollow-creek run_script --file Temp/InspectStreetTrees.cs --entry InspectStreetTrees.Main
unity command --project-path C:\Users\Zhenya\Documents\hollow-creek find_gameobjects --name "West garden garland tree"
```

`Temp/` предназначен для временных скриптов; создавайте нужный файл перед запуском. Постоянные изменения авторинга в `Assets/Scripts/Editor/Props/` требуют импорта и компиляции Unity. Для prefab используйте `PrefabUtility.LoadPrefabContents`, `SaveAsPrefabAsset` и `UnloadPrefabContents`; проверяйте изменения в экземпляре открытой сцены. Для снимков можно отрендерить временную камеру в `RenderTexture` и сохранить PNG в `Temp/`.

Документация установленного API находится в `Library/PackageCache/com.unity.pipeline@*/Documentation~/`: `index.md`, `connectivity.md`, `commands/scripts.md` и остальные справочники команд. Читайте местную документацию и справку CLI перед использованием неизвестных параметров. Не устанавливайте другую CLI или пакет для обхода ошибки доступа.

Общее окружение улицы и вида из закусочной: `Assets/Art/Environment/DinerWindow/SharedEveningStreet.prefab`. Авторинг: `EveningStreetAuthoring` и `DinerWindowAuthoring`. Добавленные деревья должны присутствовать и в prefab, и в авторинге, чтобы сохраняться при пересборке.

Оживление улицы (фонари, группы тыкв, выбор освещённых окон) — `StreetLifeAuthoring.Apply`. Его вызывает `EveningStreetAuthoring.Build`. Размещение использует фиксированный seed: хаотичная композиция сохраняется между пересборками и загрузками.

## Релизные сборки

Для сборок используйте `Assets/Editor/ReleaseBuild.cs` и `unity build` с расширенным доступом. Запускайте Windows и Linux последовательно при закрытом редакторе проекта:

```powershell
unity build . --target StandaloneWindows64 --execute-method HollowCreek.ReleaseBuild.Windows
unity build . --target StandaloneLinux64 --execute-method HollowCreek.ReleaseBuild.Linux
```

Windows собирается через IL2CPP: на этом ПК установлен Visual Studio C++ toolchain и Windows SDK `10.0.26100.0`. Обычный Windows Mono player в установке редактора отсутствует, поэтому не переключайте Windows-релиз на Mono без восстановления соответствующих файлов Unity. Для Linux установлен Linux Build Support (Mono). Сборщик явно выбирает нужный backend и предварительно собирает локальные Addressables.

Результаты и отчёты находятся в игнорируемой папке `Builds/Release`. `Tools/package_release.py` упаковывает только успешные сборки, сохраняет исполняемые права Linux и создаёт SHA-256. Не добавляйте архивы релиза, журналы, скачанные инструменты и служебные папки Unity в git; загружайте готовые архивы в GitHub Releases.

## Коммиты

Делайте коммиты по мере необходимости: после завершения и проверки логического этапа работы, чтобы изменения не накапливались в один большой коммит. Для таких коммитов отдельное подтверждение пользователя не требуется.

Перед коммитом проверяйте состав изменений и добавляйте только относящиеся к выполненной задаче файлы. Не используйте `git add -A` без проверки: исключайте временные файлы, кэш, случайные изображения и неиспользуемые промежуточные варианты ассетов. Сохраняйте изменения других задач и пользователя. Сообщение коммита должно кратко описывать результат.

После каждого коммита выполняйте `git push` в настроенную отслеживаемую ветку удалённого репозитория; отдельное подтверждение пользователя не требуется. Не используйте принудительный push. Если push не удался, сообщайте причину и не утверждайте, что изменения отправлены. Сообщайте пользователю хеш коммита и результат push.
