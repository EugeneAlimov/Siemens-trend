# SiemensTrend

Приложение для мониторинга данных ПЛК Siemens S7-1200/1500 с визуализацией в реальном времени и возможностью экспорта данных для анализа.

## Возможности

- Подключение к ПЛК Siemens S7-1200/1500 по различным протоколам (S7, OPC UA, TIA Portal Openness)
- Чтение и визуализация данных с тегов ПЛК в реальном времени
- Поддержка оптимизированных блоков данных
- Возможность создания и настройки графиков для мониторинга тегов
- Уведомления при выходе значений за заданные пределы
- Экспорт данных в CSV для последующего анализа
- Сохранение и загрузка проектов с различными конфигурациями

## Требования

- Windows 7/10/11
- .NET Framework 4.8
- TIA Portal (для функций Openness)
- Соединение с ПЛК Siemens S7-1200/1500

## Установка

1. Скачайте последнюю версию из раздела [Releases](https://github.com/EugeneAlimov/SiemensTrend/releases)
2. Распакуйте архив в любую директорию
3. Запустите файл `SiemensTrend.exe`

## Использование

### Подключение к ПЛК

1. Запустите приложение
2. Создайте новый проект или откройте существующий
3. Укажите параметры подключения к ПЛК (IP-адрес, тип CPU, rack, slot)
4. Нажмите "Подключиться"

### Выбор тегов для мониторинга

1. После успешного подключения загрузите структуру тегов из ПЛК
2. Выберите теги для мониторинга из древовидной структуры
3. Настройте параметры мониторинга (интервал опроса, уведомления)

### Визуализация данных

1. Создайте новый график, добавив выбранные теги
2. Настройте параметры отображения (цвета, масштаб, временной диапазон)
3. Используйте панель управления для управления графиком (пауза, масштабирование, маркеры)

### Экспорт данных

1. Выберите "Экспорт" в меню
2. Укажите период данных для экспорта
3. Выберите формат и место сохранения файла
4. Нажмите "Экспортировать"

## Разработка

### Требования для разработки

- Visual Studio 2019/2022
- .NET Framework 4.8 SDK

### Настройка окружения разработки

1. Клонируйте репозиторий:
   ```
   git clone https://github.com/yourusername/SiemensTrend.git
   ```

2. Откройте решение в Visual Studio:
   ```
   SiemensTrend.sln
   ```

3. Установите необходимые NuGet пакеты (при необходимости):
   ```
   S7NetPlus
   LiveChartsCore.SkiaSharpView.WPF
   ```

## Лицензия

MIT License

## Авторы

- EugeneAlimov
