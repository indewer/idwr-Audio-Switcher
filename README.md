# (idwr) Audio Switcher

[![Version](https://img.shields.io/badge/version-0.2%20b2-indigo.svg)](#)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-blue.svg)](#)
[![Author](https://img.shields.io/badge/author-@indewer-blueviolet.svg)](https://t.me/indewer)

A sleek, lightweight floating widget designed for Windows 10 & 11 to solve the hassle of the default audio tray menu.

---

## English

### Key Features
- ⚡ **Instant Switching:** Change default playback devices with a single click without opening bulky system menus.
- 🎯 **Device Filtering:** Show only the devices you actually use (hide monitors, digital outputs, virtual cables).
- 🔌 **Auto-Detect & Multi-Endpoint:** Connected devices (USB, HDMI TV, discrete soundcards) appear automatically and keep their custom names.
- 🏷️ **Custom Names:** Rename cryptic hardware names to clean labels (e.g. "Headphones", "Speakers", "TV").
- ⏱️ **Smart MRU Sorting:** Frequently and recently used outputs dynamically rise to the top of the list.
- 🎨 **Windows 11 Fluent UI:** Dark glassmorphism card, smooth animations, and matching custom system tray icon.
- 📌 **Always-on-Top Indicator:** Clear visual pin state and instant on-top toggling.
- 🔄 **Auto-Save & Multi-Monitor:** Window position is accurately remembered across multiple screens and restarts.
- 🚀 **Smart Auto-Updater:** Instant check on launch for settings badge + 12-day launch notification popup ("Update" / "Remind Later").

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
**Версия:** 0.2 b2  

### Возможности
1. **Выбор только нужных устройств**: Нажмите на шестеренку ⚙️ в заголовке виджета и отключите ненужные выходы (например, лишние разъемы или мониторы).
2. **Автоопределение USB и мультивыходов**: Любые подключенные наушники, ТВ (LG TV) или колонки (Sound Blaster Z) сразу появляются в списке, а их имена сохраняются даже при переподключении.
3. **Свои понятные имена**: Каждому устройству можно задать короткое имя прямо в настройках (например, «Наушники» вместо «Динамики (1MORE Spearhead VR)»).
4. **Мгновенное переключение**: Клик по карточке устройства моментально делает его выводом по умолчанию для всей системы.
5. **Умная сортировка**: Недавно и часто используемые устройства автоматически поднимаются в начало списка.
6. **Статус «Активно»**: Текущий активный аудиовыход подсвечивается индиго-свечением и маркером «Активно».
7. **Закрепление поверх окон (📌)**: Кнопка-булавка в шапке всегда видна и подсвечивается синим при закреплении.
8. **Перетаскивание и мультимониторность**: Виджет надежно запоминает экран и координаты даже в многомониторных конфигурациях.
9. **Сворачивание в трей**: Кнопка «─» или «✕» сворачивает виджет в системный трей (возле часов). Двойной клик по фирменной иконке в трее открывает его снова.
10. **Умный автоапдейтер**: Быстрая тихая проверка обновлений при каждом старте для бейджа в настройках, плюс ненавязчивое всплывающее уведомление раз в 12 дней с кнопками «Обновить» и «Напомнить позже».
11. **Автозагрузка**: В настройках можно включить «Запускать при старте Windows».
12. **Автосохранение**: Любые изменения настроек и позиций сохраняются мгновенно на лету.
11. **Автозагрузка**: В настройках можно включить «Запускать при старте Windows».
12. **Автосохранение**: Любые изменения настроек и позиций сохраняются мгновенно на лету.

### Запуск
* Запустите файл `AudioSwitcherWidget\publish\AudioSwitcherWidget.exe` напрямую или через скрипт `Запустить AudioSwitcher.bat`.
