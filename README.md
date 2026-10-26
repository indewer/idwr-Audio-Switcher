# (idwr) Audio Switcher

[![Version](https://img.shields.io/badge/version-0.2%20b1-indigo.svg)](#)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-blue.svg)](#)
[![Author](https://img.shields.io/badge/author-@indewer-blueviolet.svg)](https://t.me/indewer)

A sleek, lightweight floating widget designed for Windows 10 & 11 to solve the hassle of the default audio tray menu.

---

## English

### Key Features
- ⚡ **Instant Switching:** Change default playback devices with a single click without opening bulky system menus.
- 🎯 **Device Filtering:** Show only the devices you actually use (hide monitors, digital outputs, virtual cables).
- 🔌 **Auto-Detect & Reconnect:** Newly connected and USB devices (e.g. 1MORE headsets) appear automatically and keep their custom names even if re-plugged into different ports.
- 🏷️ **Custom Names:** Rename cryptic hardware names to clean labels (e.g. "Headphones", "Speakers", "TV").
- ⏱️ **Smart MRU Sorting:** Frequently and recently used outputs dynamically rise to the top of the list.
- 🎨 **Windows 11 Fluent UI:** Dark glassmorphism card, smooth animations, and matching custom system tray icon.
- 📌 **Always-on-Top & Portable:** Drag anywhere on screen, pin over windows, or minimize to the system tray.
- 🔄 **Auto-Save & Multi-Monitor:** Window position is accurately remembered across multiple screens and restarts.
- 🚀 **Auto-Updater:** Periodic background check (every 12 days) with unobtrusive 1-click update badge.

### Running & Building
- **Direct Launch:** Run `publish/AudioSwitcherWidget.exe`.
- **Build from Source:**
  ```bash
  dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
  ```

---

## Русский

Стильный, быстрый и компактный плавающий виджет для мгновенного переключения аудиовыходов в Windows 10/11 в один клик.

**Автор:** [@indewer](https://t.me/indewer) (indewer@ya.ru)  
**Версия:** 0.2 b1  

### Возможности
1. **Выбор только нужных устройств**: Нажмите на шестеренку ⚙️ в заголовке виджета и отключите ненужные выходы (например, лишние HDMI разъемы или мониторы).
2. **Автоопределение USB и новых устройств**: Любые новые подключенные наушники или колонки сразу появляются в списке, а их заданные имена сохраняются даже при переподключении USB.
3. **Свои понятные имена**: Каждому устройству можно задать короткое имя прямо в настройках (например, «Наушники» вместо «Динамики (1MORE Spearhead VR)»).
4. **Мгновенное переключение**: Клик по карточке устройства моментально делает его выводом по умолчанию для всей системы.
5. **Умная сортировка**: Недавно и часто используемые устройства автоматически поднимаются в начало списка.
6. **Статус «Активно»**: Текущий активный аудиовыход подсвечивается индиго-свечением и маркером «Активно».
7. **Закрепление поверх окон (📌)**: Кнопка-булавка в шапке включает режим «Поверх всех окон», чтобы виджет всегда был под рукой.
8. **Перетаскивание и мультимониторность**: Виджет надежно запоминает экран и координаты даже в многомониторных конфигурациях.
9. **Сворачивание в трей**: Кнопка «─» или «✕» сворачивает виджет в системный трей (возле часов). Двойной клик по фирменной иконке в трее открывает его снова.
10. **Автообновление**: Ненавязчивая автоматическая проверка обновлений на GitHub (раз в 12 дней) с обновлением в один клик прямо из настроек.
11. **Автозагрузка**: В настройках можно включить «Запускать при старте Windows».
12. **Автосохранение**: Любые изменения настроек и позиций сохраняются мгновенно на лету.

### Запуск
* Запустите файл `AudioSwitcherWidget\publish\AudioSwitcherWidget.exe` напрямую или через скрипт `Запустить AudioSwitcher.bat`.
