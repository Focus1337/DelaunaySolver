# Приложение для вычисления и отображения результата триангуляции Делоне.

## Описание работы
Используется алгоритм инкрементальной триангуляции Делоне. Этот алгоритм строит триангуляцию постепенно, добавляя каждую точку по очереди и обновляя существующую триангуляцию.

Шаги:
* Сортировка точек: Сначала точки сортируются по их координатам x. Это позволяет эффективно обрабатывать точки в порядке возрастания x-координаты.
* Построение начальной триангуляции: Первые три точки добавляются в триангуляцию в качестве начального треугольника. Это обычно делается путем создания супертреугольника, который содержит все точки.
* Добавление точек: Каждая оставшаяся точка добавляется по очереди. Для каждой точки выполняются следующие шаги:
  * Определение текущего треугольника: Точка ищет треугольник в триангуляции, внутри которого она находится.
  * Формирование новых ребер: Точка соединяется с каждым ребром текущего треугольника, образуя новые треугольники.
  * Обновление триангуляции: Удаляются треугольники, которые перекрываются новыми треугольниками. Это включает удаление ребер и обновление связей между треугольниками.
  * Проверка допустимости триангуляции: Проверяется условие Делоне для новых треугольников, чтобы гарантировать их корректность.
* Завершение: После добавления всех точек в триангуляцию удаляется супертреугольник и возвращается массив индексов вершин треугольников.

Эффективность построения триангуляции имеет временную сложность O(n log n), где n - количество точек. Это делает его подходящим для обработки больших наборов точек в реальном времени.
Пример с 1 млн. точек ниже.

## Генерация точек
Используется алгоритм Fast Uniform Poisson-Disk Sampling (FUDS). Это алгоритм для генерации равномерно распределенных точек в двумерном пространстве с минимальным расстоянием между точками.

Алгоритм FUDS использует идею разбиения пространства на ячейки с определенным размером, которые помогают обеспечить равномерное распределение точек. Он работает следующим образом:
* Инициализация:
  * Создается сетка ячеек, которая покрывает всю область генерации точек.
  * Создается список активных ячеек, который изначально содержит одну случайную ячейку.
  * Создается список сгенерированных точек, который изначально пуст.
* Генерация точек:
  * Из списка активных ячеек выбирается случайная ячейка.
  * Внутри выбранной ячейки генерируется случайная точка.
  * Проверяется, удовлетворяет ли новая точка условиям равномерного распределения:
    * Она должна быть достаточно удалена от других точек (минимальное расстояние).
    * Она должна быть достаточно близка к точкам вокруг выбранной ячейки (максимальное расстояние).
  * Если новая точка удовлетворяет условиям, она добавляется в список сгенерированных точек и в список активных ячеек.
  * Если новая точка не удовлетворяет условиям, она отбрасывается.
* Обновление списка активных ячеек:
  * Если выбранная ячейка содержит достаточное количество точек, она удаляется из списка активных ячеек.
  * Если выбранная ячейка все еще активна, она остается в списке активных ячеек.
  * Повторение шагов 2-3 до тех пор, пока список активных ячеек не станет пустым.

## Документация
Каждый метод в алгоритма триангуляции задокументирован здесь
[Delaunay/Triangulator.cs](https://github.com/Focus1337/DelaunaySolver/blob/main/Delaunay/Triangulator.cs)

## Примеры работы
![Example 1](Renders/Example.png "Title")
Триангуляция 1 млн. точек
![Example 2](Renders/Million%20points.png "Title")
Экспортированные рендеры
![Exported 1](Renders/t-result-10-07-2023-1-19-18.png "Title")
![Exported 2](Renders/t-result-10-07-2023-12-37-13.png "Title")

## Benchmark
![Benchmark](Renders/Benchmark.png "Title")
