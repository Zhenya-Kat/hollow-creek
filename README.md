# HollowCreek

3D-игра на Unity.

- **Unity:** 6000.6.3f1 (Unity 6)
- **Рендер:** Universal Render Pipeline (URP)
- **Ввод:** Input System

## Структура проекта

```
Assets/
  Art/
    Models/       3D-модели (.fbx)
    Textures/     текстуры
    Materials/    материалы
  Audio/
    Music/        музыка
    SFX/          звуковые эффекты
  Prefabs/        префабы
  Scenes/         сцены
  Scripts/        C#-скрипты
  Settings/       настройки URP
SourceArt/        исходники (.blend, .psd) — Unity их не импортирует
Packages/         подключённые пакеты
ProjectSettings/  настройки проекта
```

## Установка на новом компьютере

### 1. Инструменты (один раз)

1. Установить [Git for Windows](https://git-scm.com/) (Git LFS входит в комплект).
2. Выполнить:
   ```
   git lfs install
   git config --global user.name "Ваше Имя"
   git config --global user.email "ваша@почта"
   ```
3. Установить [Unity Hub](https://unity.com/download) и через него редактор **6000.6.3f1**.
   Нужна именно эта версия (см. `ProjectSettings/ProjectVersion.txt`).
4. Установить редактор кода: Visual Studio (компонент «Game development with Unity»), Rider или VS Code.

### 2. Клонирование

```
git clone <url-репозитория>
```

### 3. Открытие в Unity

1. Unity Hub → **Add** → **Add project from disk** → выбрать папку проекта.
2. Первое открытие долгое: Unity заново собирает папку `Library`. Это нормально.
3. Если открылась пустая сцена `Untitled` — открыть нужную из `Assets/Scenes`.
4. Edit → Preferences → External Tools → **External Script Editor** — выбрать свою IDE.

## Работа с репозиторием

- **Перед работой:** `git pull`
- **После работы:** сохранить сцену (Ctrl+S) и проект (File → Save Project), затем commit и `git push`.
- Перемещать и переименовывать файлы **только внутри Unity** (окно Project), иначе потеряются `.meta`-файлы и сломаются ссылки.
- Не редактировать одну и ту же сцену/префаб на двух компьютерах без синхронизации — конфликты в сценах сложно разрешать.

### Git LFS

Большие бинарные файлы (модели, текстуры, аудио, видео, шрифты) автоматически хранятся в Git LFS — список расширений в `.gitattributes`.
Если добавляете файл нового бинарного формата, добавьте его расширение в `.gitattributes`.

Проверить, какие файлы в LFS: `git lfs ls-files`

## Решение проблем

| Проблема | Решение |
|---|---|
| Модели розовые / текстуры не открываются | Не скачались LFS-файлы: `git lfs pull` |
| Unity предлагает обновить проект | Установлена другая версия редактора — поставьте 6000.6.3f1 |
| IDE не подсвечивает Unity API | Проверьте External Script Editor в настройках Unity, затем Assets → Open C# Project |
