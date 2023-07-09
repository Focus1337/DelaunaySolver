using Delaunay.Interfaces;
using Delaunay.Models;

// ReSharper disable CommentTypo

namespace Delaunay;

public class Triangulator
{
    private readonly double _epsilon = Math.Pow(2, -52);

    // Это массив, используемый во время операции легализации ребер.
    // Он служит в качестве стека для хранения индексов полуребер, которые требуют дальнейшей обработки.
    private readonly int[] _edgeStack = new int[512];

    // Массив, который хранит индексы точек, образующих треугольники. Каждые три последовательных элемента в массиве представляют один треугольник.
    private int[] Triangles { get; }

    // Массив, который хранит индексы полуребер. Каждое полуребро связывает две точки и указывает на индекс соседнего полуребра.
    private int[] Halfedges { get; }
    public IPoint[] Points { get; }

    // Массив, который хранит индексы точек, образующих выпуклую оболочку. Эти точки образуют внешний контур триангуляции.
    private int[] Hull { get; }
    private readonly int _hashSize;

    // Массивы, используемые для хранения информации о выпуклой оболочке. Они содержат индексы предыдущего и следующего полуребер,
    // а также индексы треугольников и хэш-таблицу для быстрого доступа к полуребрам выпуклой оболочки.
    private readonly int[] _hullPrev;
    private readonly int[] _hullNext;
    private readonly int[] _hullTri;
    private readonly int[] _hullHash;

    // Координаты центра триангуляции, которые используются для вычисления ориентации точек и определения выпуклой оболочки.
    private readonly double _centerX;
    private readonly double _centerY;

    private int _trianglesLen;
    private readonly double[] _coords;
    private readonly int _hullStart;
    private readonly int _hullSize;

    /// <summary>
    /// Алгоритм инкрементальной триангуляции Делоне. Этот алгоритм строит триангуляцию постепенно,
    /// добавляя каждую точку по очереди и обновляя существующую триангуляцию.
    /// </summary>
    public Triangulator(IPoint[] points)
    {
        if (points.Length < 3)
            throw new ArgumentOutOfRangeException("Need at least 3 points");

        Points = points;
        _coords = new double[Points.Length * 2];

        for (var i = 0; i < Points.Length; i++)
        {
            var p = Points[i];
            _coords[2 * i] = p.X;
            _coords[2 * i + 1] = p.Y;
        }

        var pointsCount = points.Length;
        var maxTriangles = 2 * pointsCount - 5;

        Triangles = new int[maxTriangles * 3];

        Halfedges = new int[maxTriangles * 3];
        _hashSize = (int)Math.Ceiling(Math.Sqrt(pointsCount));

        _hullPrev = new int[pointsCount];
        _hullNext = new int[pointsCount];
        _hullTri = new int[pointsCount];
        _hullHash = new int[_hashSize];

        var ids = new int[pointsCount];

        var minX = double.PositiveInfinity;
        var minY = double.PositiveInfinity;
        var maxX = double.NegativeInfinity;
        var maxY = double.NegativeInfinity;

        // вычисляем минимальные и максимальные значения координат точек для определения центра триангуляции.
        for (var i = 0; i < pointsCount; i++)
        {
            var x = _coords[2 * i];
            var y = _coords[2 * i + 1];
            if (x < minX) minX = x;
            if (y < minY) minY = y;
            if (x > maxX) maxX = x;
            if (y > maxY) maxY = y;
            ids[i] = i;
        }

        var centerX = (minX + maxX) / 2;
        var centerY = (minY + maxY) / 2;

        var minDist = double.PositiveInfinity;
        var i0 = 0;
        var i1 = 0;
        var i2 = 0;

        // выбираем начальную (i0) ближайшую точку к центру триангуляции
        for (var i = 0; i < pointsCount; i++)
        {
            var d = Dist(centerX, centerY, _coords[2 * i], _coords[2 * i + 1]);
            if (d < minDist)
            {
                i0 = i;
                minDist = d;
            }
        }

        var i0x = _coords[2 * i0];
        var i0y = _coords[2 * i0 + 1];

        minDist = double.PositiveInfinity;

        // находим вторую точку
        for (var i = 0; i < pointsCount; i++)
        {
            if (i == i0) continue;
            var d = Dist(i0x, i0y, _coords[2 * i], _coords[2 * i + 1]);
            if (d < minDist && d > 0)
            {
                i1 = i;
                minDist = d;
            }
        }

        var i1x = _coords[2 * i1];
        var i1y = _coords[2 * i1 + 1];

        var minRadius = double.PositiveInfinity;

        // и находим третью; эти точки образуют наименьший описывающий окружности треугольник с первой точкой.
        for (var i = 0; i < pointsCount; i++)
        {
            if (i == i0 || i == i1) continue;
            var r = Circumradius(i0x, i0y, i1x, i1y, _coords[2 * i], _coords[2 * i + 1]);
            if (r < minRadius)
            {
                i2 = i;
                minRadius = r;
            }
        }

        var i2x = _coords[2 * i2];
        var i2y = _coords[2 * i2 + 1];

        // Проверяем, существует ли такой треугольник.
        if (double.IsPositiveInfinity(minRadius))
            throw new Exception("No Delaunay triangulation exists for this input.");

        if (Orient(i0x, i0y, i1x, i1y, i2x, i2y))
        {
            var i = i1;
            var x = i1x;
            var y = i1y;
            i1 = i2;
            i1x = i2x;
            i1y = i2y;
            i2 = i;
            i2x = x;
            i2y = y;
        }

        var center = Circumcenter(i0x, i0y, i1x, i1y, i2x, i2y);
        _centerX = center.X;
        _centerY = center.Y;

        var dists = new double[pointsCount];
        for (var i = 0; i < pointsCount; i++)
            dists[i] = Dist(_coords[2 * i], _coords[2 * i + 1], center.X, center.Y);

        // сортируем точки по расстоянию от центра окружности начального треугольника
        Quicksort(ids, dists, 0, pointsCount - 1);

        // устанавливаем начальную выпуклую оболочку, состоящую из начального треугольника
        _hullStart = i0;
        _hullSize = 3;

        _hullNext[i0] = _hullPrev[i2] = i1;
        _hullNext[i1] = _hullPrev[i0] = i2;
        _hullNext[i2] = _hullPrev[i1] = i0;

        _hullTri[i0] = 0;
        _hullTri[i1] = 1;
        _hullTri[i2] = 2;

        _hullHash[HashKey(i0x, i0y)] = i0;
        _hullHash[HashKey(i1x, i1y)] = i1;
        _hullHash[HashKey(i2x, i2y)] = i2;

        _trianglesLen = 0;
        AddTriangle(i0, i1, i2, -1, -1, -1);

        double xp = 0;
        double yp = 0;

        for (var k = 0; k < ids.Length; k++)
        {
            var i = ids[k];
            var x = _coords[2 * i];
            var y = _coords[2 * i + 1];

            // skip near-duplicate points
            if (k > 0 && Math.Abs(x - xp) <= _epsilon && Math.Abs(y - yp) <= _epsilon) continue;
            xp = x;
            yp = y;

            // skip seed triangle points
            if (i == i0 || i == i1 || i == i2) continue;

            // находим видимое ребро на выпуклой оболочке, используя хэш-таблицу ребер
            var start = 0;
            for (var j = 0; j < _hashSize; j++)
            {
                var key = HashKey(x, y);
                start = _hullHash[(key + j) % _hashSize];
                if (start != -1 && start != _hullNext[start]) break;
            }

            start = _hullPrev[start];
            var e = start;
            var q = _hullNext[e];

            while (!Orient(x, y, _coords[2 * e], _coords[2 * e + 1], _coords[2 * q], _coords[2 * q + 1]))
            {
                e = q;
                if (e == start)
                {
                    e = int.MaxValue;
                    break;
                }

                q = _hullNext[e];
            }

            if (e == int.MaxValue) continue; // likely a near-duplicate point; skip it

            // Добавляем новый треугольник, связанный с точкой
            var t = AddTriangle(e, i, _hullNext[e], -1, -1, _hullTri[e]);

            // выполняем рекурсивное переворачивание треугольников до выполнения условия Делоне
            _hullTri[i] = Legalize(t + 2);
            _hullTri[e] = t; // keep track of boundary triangles on the hull
            _hullSize++;

            // walk forward through the hull, adding more triangles and flipping recursively
            var next = _hullNext[e];
            q = _hullNext[next];

            while (Orient(x, y, _coords[2 * next], _coords[2 * next + 1], _coords[2 * q], _coords[2 * q + 1]))
            {
                t = AddTriangle(next, i, q, _hullTri[i], -1, _hullTri[next]);
                _hullTri[i] = Legalize(t + 2);
                _hullNext[next] = next; // mark as removed
                _hullSize--;
                next = q;

                q = _hullNext[next];
            }

            // walk backward from the other side, adding more triangles and flipping
            if (e == start)
            {
                q = _hullPrev[e];

                while (Orient(x, y, _coords[2 * q], _coords[2 * q + 1], _coords[2 * e], _coords[2 * e + 1]))
                {
                    t = AddTriangle(q, i, e, -1, _hullTri[e], _hullTri[q]);
                    Legalize(t + 2);
                    _hullTri[q] = t;
                    _hullNext[e] = e; // mark as removed
                    _hullSize--;
                    e = q;

                    q = _hullPrev[e];
                }
            }

            // обновляем индексы выпуклой оболочки
            _hullStart = _hullPrev[i] = e;
            _hullNext[e] = _hullPrev[next] = i;
            _hullNext[i] = next;

            // и хэш-таблицу ребер
            _hullHash[HashKey(x, y)] = i;
            _hullHash[HashKey(_coords[2 * e], _coords[2 * e + 1])] = e;
        }

        // ФормируеМ массив Hull, содержащий индексы точек выпуклой оболочки.
        Hull = new int[_hullSize];
        var s = _hullStart;
        for (var i = 0; i < _hullSize; i++)
        {
            Hull[i] = s;
            s = _hullNext[s];
        }

        _hullPrev = _hullNext = _hullTri = null; // удалим лишние массивы

        // обрезаем массивы Triangles и Halfedges, чтобы они содержали только треугольники, созданные во время триангуляции.
        Triangles = Triangles.Take(_trianglesLen).ToArray();
        Halfedges = Halfedges.Take(_trianglesLen).ToArray();
    }

    /// <summary>
    /// Метод, который позволяет произвести действие с каждым ребром треугольника с помощью колбэка 
    /// </summary>
    public void ForEachTriangleEdge(Action<IEdge> callback)
    {
        foreach (var edge in GetEdges())
            callback?.Invoke(edge);
    }

    /// <summary>
    /// Выполняет операцию "легализации" ребра в триангуляции. Эта операция гарантирует, что ребро удовлетворяет
    /// условию Делоне, то есть точка p1 не находится в окружности, описанной вокруг треугольника,
    /// образованного точками p0, pl и pr. Если ребро не удовлетворяет условию Делоне, оно "переворачивается" путем замены точек p0 и p1 местами.
    /// </summary>
    private int Legalize(int a)
    {
        var i = 0;
        int ar;

        // рекурсия устраняется благодаря стеку фиксированного размера edgeStack
        while (true)
        {
            // получаем связанное с a полуребро b
            var b = Halfedges[a];

            /* если пара треугольников не удовлетворяет условию Делоне
             * (p1 находится внутри описанной окружности [p0, pl, pr]), перевернем их,
             * затем выполним ту же проверку/переворот рекурсивно для новой пары треугольников
             *
             *           pl                    pl
             *          /||\                  /  \
             *       al/ || \bl            al/    \a
             *        /  ||  \              /      \
             *       /  a||b  \    flip    /___ar___\
             *     p0\   ||   /p1   =>   p0\---bl---/p1
             *        \  ||  /              \      /
             *       ar\ || /br             b\    /br
             *          \||/                  \  /
             *           pr                    pr
             */
            var a0 = a - a % 3;
            ar = a0 + (a + 2) % 3;

            if (b == -1)
            {
                // convex hull edge
                if (i == 0) break;
                a = _edgeStack[--i];
                continue;
            }

            var b0 = b - b % 3;
            var al = a0 + (a + 1) % 3;
            var bl = b0 + (b + 2) % 3;

            // находим индексы точек треугольника p
            var p0 = Triangles[ar];
            var pr = Triangles[a];
            var pl = Triangles[al];
            var p1 = Triangles[bl];

            // проверка "внутри окружности?"
            var illegal = InCircle(
                _coords[2 * p0], _coords[2 * p0 + 1],
                _coords[2 * pr], _coords[2 * pr + 1],
                _coords[2 * pl], _coords[2 * pl + 1],
                _coords[2 * p1], _coords[2 * p1 + 1]);

            if (illegal)
            {
                // если внутри, то делаем свап p0 и p1
                Triangles[a] = p1;
                Triangles[b] = p0;

                var hbl = Halfedges[bl];
                if (hbl == -1)
                {
                    var e = _hullStart;
                    do
                    {
                        if (_hullTri[e] == bl)
                        {
                            _hullTri[e] = a;
                            break;
                        }

                        e = _hullPrev[e];
                    } while (e != _hullStart);
                }

                // Обновляем ссылки на полуребра, связанные с ребром.
                // Если полуребро bl (связанное с ребром на другой стороне оболочки) не имеет ссылки на полуребро,
                // метод выполняет поиск полуребра bl в оболочке и обновляет его ссылку на a.
                Link(a, hbl);
                Link(b, Halfedges[ar]);
                Link(ar, bl);

                var br = b0 + (b + 1) % 3;

                // Добавляет индекс полуребра br (связанного с ребром на другой стороне) в стек _edgeStack для последующей обработки.
                if (i < _edgeStack.Length)
                    _edgeStack[i++] = br;
            }
            else
            {
                // проверяем, есть ли ещё индексы полуребер в стеке, если нет - процесс легализации пройден
                if (i == 0) break;
                a = _edgeStack[--i];
            }
        }

        return ar;
    }

    /// <summary>
    /// Для определения, находится ли точка P внутри окружности, описанной вокруг треугольника с вершинами A, B и C.
    /// Метод возвращает true, если точка P находится внутри окружности, и false в противном случае.
    /// </summary>
    /// <param name="ax">Коорд X точки A</param>
    /// <param name="ay">Коорд Y точки A</param>
    /// <returns>Находится или нет внутри окружности</returns>
    private static bool InCircle(double ax, double ay, double bx, double by, double cx, double cy, double px, double py)
    {
        var dx = ax - px;
        var dy = ay - py;
        var ex = bx - px;
        var ey = by - py;
        var fx = cx - px;
        var fy = cy - py;

        var ap = dx * dx + dy * dy;
        var bp = ex * ex + ey * ey;
        var cp = fx * fx + fy * fy;

        return dx * (ey * cp - bp * fy) -
            dy * (ex * cp - bp * fx) +
            ap * (ex * fy - ey * fx) < 0;
    }

    /// <summary>
    /// Для добавления нового треугольника в список треугольников и установки связей между треугольниками.
    /// Аргументы метода i0, i1 и i2 представляют индексы вершин треугольника в списке вершин.
    /// Аргументы a, b и c представляют индексы смежных треугольников.
    /// </summary>
    /// <returns>Индекс нового треугольника t</returns>
    private int AddTriangle(int i0, int i1, int i2, int a, int b, int c)
    {
        var t = _trianglesLen;

        Triangles[t] = i0;
        Triangles[t + 1] = i1;
        Triangles[t + 2] = i2;

        Link(t, a);
        Link(t + 1, b);
        Link(t + 2, c);

        _trianglesLen += 3;
        return t;
    }

    /// <summary>
    /// Используется для установки связи между двумя ребрами треугольников.
    /// </summary>
    /// <param name="a">Индекс ребра в списке полуребер (одна сторона треуг)</param>
    /// <param name="b">Индекс ребра в списке полуребер (смежная сторона треуг)</param>
    private void Link(int a, int b)
    {
        Halfedges[a] = b;
        if (b != -1) Halfedges[b] = a;
    }

    /// <summary>
    /// Используется для хэширования и сортировки вершин треугольников.
    /// Принимает координаты x и y вершины и возвращает хеш-ключ, который используется для индексации вершин в хэш-таблице.
    /// Хеш-таблица используется для быстрого поиска ближайших соседей вершин.
    /// </summary>
    private int HashKey(double x, double y) =>
        (int)(Math.Floor(PseudoAngle(x - _centerX, y - _centerY) * _hashSize) % _hashSize);

    /// <summary>
    /// Принимает разности dx и dy координат и вычисляет псевдоугол, который используется для сортировки вершин.
    /// </summary>
    private static double PseudoAngle(double diffX, double diffY)
    {
        var normalizedDiffX = diffX / (Math.Abs(diffX) + Math.Abs(diffY));
        return (diffY > 0 ? 3 - normalizedDiffX : 1 + normalizedDiffX) / 4;
    }

    private static void Quicksort(int[] ids, double[] dists, int left, int right)
    {
        if (right - left <= 20)
        {
            for (var i = left + 1; i <= right; i++)
            {
                var temp = ids[i];
                var tempDist = dists[temp];
                var j = i - 1;
                while (j >= left && dists[ids[j]] > tempDist) ids[j + 1] = ids[j--];
                ids[j + 1] = temp;
            }
        }
        else
        {
            var median = (left + right) >> 1;
            var i = left + 1;
            var j = right;
            Swap(ids, median, i);
            if (dists[ids[left]] > dists[ids[right]]) Swap(ids, left, right);
            if (dists[ids[i]] > dists[ids[right]]) Swap(ids, i, right);
            if (dists[ids[left]] > dists[ids[i]]) Swap(ids, left, i);

            var temp = ids[i];
            var tempDist = dists[temp];
            while (true)
            {
                do i++;
                while (dists[ids[i]] < tempDist);
                do j--;
                while (dists[ids[j]] > tempDist);
                if (j < i) break;
                Swap(ids, i, j);
            }

            ids[left + 1] = ids[j];
            ids[j] = temp;

            if (right - i + 1 >= j - left)
            {
                Quicksort(ids, dists, i, right);
                Quicksort(ids, dists, left, j - 1);
            }
            else
            {
                Quicksort(ids, dists, left, j - 1);
                Quicksort(ids, dists, i, right);
            }
        }
    }

    private static void Swap(int[] arr, int i, int j) =>
        (arr[i], arr[j]) = (arr[j], arr[i]);

    /// <summary>
    /// Для определения ориентации трех точек в плоскости.
    /// Он принимает координаты трех точек (px, py), (qx, qy) и (rx, ry) и возвращает булевое значение,
    /// указывающее на направление обхода этих точек.
    /// </summary>
    /// <returns>Если значение выражения больше или равно нулю, то это означает, что тройка точек образует
    /// поворот вправо (по часовой стрелке) или лежит на одной прямой. В этом случае метод возвращает false.
    /// Если значение выражения меньше нуля, то это означает, что тройка точек (px, py), (qx, qy) и (rx, ry)
    /// образует поворот влево (против часовой стрелки). В этом случае метод возвращает true.</returns>
    private static bool Orient(double px, double py, double qx, double qy, double rx, double ry) =>
        (qy - py) * (rx - qx) - (qx - px) * (ry - qy) < 0;

    /// <summary>
    /// Для вычисления радиуса описанной окружности треугольника.
    /// Он принимает координаты трех вершин треугольника (ax, ay), (bx, by) и (cx, cy)
    /// </summary>
    /// <returns>возвращает квадрат радиуса описанной окружности</returns>
    private static double Circumradius(double ax, double ay, double bx, double by, double cx, double cy)
    {
        var dx = bx - ax;
        var dy = by - ay;
        var ex = cx - ax;
        var ey = cy - ay;
        var bl = dx * dx + dy * dy;
        var cl = ex * ex + ey * ey;
        var d = 0.5 / (dx * ey - dy * ex);
        var x = (ey * bl - dy * cl) * d;
        var y = (dx * cl - ex * bl) * d;
        return x * x + y * y;
    }

    /// <summary>
    /// Для вычисления центра описанной окружности треугольника.
    /// Он принимает координаты трех вершин треугольника (ax, ay), (bx, by) и (cx, cy)
    /// </summary>
    /// <returns>возвращает объект Point, представляющий координаты центра описанной окружности</returns>
    private static Point Circumcenter(double ax, double ay, double bx, double by, double cx, double cy)
    {
        var dx = bx - ax;
        var dy = by - ay;
        var ex = cx - ax;
        var ey = cy - ay;
        var bl = dx * dx + dy * dy;
        var cl = ex * ex + ey * ey;
        var d = 0.5 / (dx * ey - dy * ex);
        var x = ax + (ey * bl - dy * cl) * d;
        var y = ay + (dx * cl - ex * bl) * d;

        return new Point(x, y);
    }

    private static double Dist(double ax, double ay, double bx, double by)
    {
        var diffX = ax - bx;
        var diffY = ay - by;
        return diffX * diffX + diffY * diffY;
    }

    /// <summary>
    /// Для получения списка ребер (экземпляров IEdge) триангуляции.
    /// Он выполняет итерацию по массиву Triangles, который содержит индексы точек треугольников,
    /// и создает ребра между точками треугольников.
    /// </summary>
    private IEnumerable<IEdge> GetEdges()
    {
        for (var e = 0; e < Triangles.Length; e++)
            if (e > Halfedges[e])
            {
                var p = Points[Triangles[e]];
                var q = Points[Triangles[NextHalfedge(e)]];
                yield return new Edge(e, p, q);
            }
    }

    /// <summary>
    /// Для получения индекса следующего полуребра в триангуляции.
    /// </summary>
    private static int NextHalfedge(int e) => (e % 3 == 2) ? e - 2 : e + 1;
}