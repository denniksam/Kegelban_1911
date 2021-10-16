using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.UI;   // Text, Image, Button

public class Controls : MonoBehaviour
{
    private const float MIN_FORCE = 1000f;
    private const float MAX_FORCE = 2000f;
    private const string BEST_RES_FILE = "best.xml";

    private GameObject Ball;
    private Rigidbody ballRigidbody;
    private Vector3 ballStartPosition;
    private bool isBallMoving;

    private GameObject Arrow;
    private GameObject ArrowTail;
    private float arrowAngle;  // угол поворота стрелки
    private float maxArrowAngle = 20f;  // граничный угол поворота

    private Image ForceIndicator;   // Ссылка на Image ForceIndicator

    private Text GameStat;
    private int attempt;

    private GameObject GameMenu;

    private List<GameResult> bestResults;  // таблица рекордов

    void Start()
    {
        LoadBestResults();

        GameMenu = GameObject.Find("Menu");
        Menu.MenuMode = MenuMode.Start;

        attempt = 0;
        GameStat = GameObject.Find("GameStat").GetComponent<Text>();

        // Получаем ссылку на компонент Image объекта ForceIndicator
        ForceIndicator = GameObject.Find("ForceIndicator").GetComponent<Image>();

        Arrow = GameObject.Find("Arrow");
        ArrowTail = GameObject.Find("ArrowTail");
        arrowAngle = 0f;

        // Находим шарик
        Ball = GameObject.Find("Ball");  // по имени в иерархии
        // сохраняем его стартовую позицию
        ballStartPosition = Ball.transform.position;
        // сохраняем ссылку на его тв.т
        ballRigidbody = Ball.GetComponent<Rigidbody>();
        isBallMoving = false;
    }

    // Update is called once per frame
    void Update()
    {
        #region Взаимодействие с меню
        // если меню активно - не выполнять никаких действий
        if (Menu.IsActive) return;

        // если нажата Escape - активировать меню
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            GameMenu.SetActive(true);
            Menu.IsActive = true;
            Menu.MenuMode = MenuMode.Pause;
        }
        #endregion

        #region Остановка шарика
        if (ballRigidbody.velocity.magnitude < 0.1f && isBallMoving)
        {            
            isBallMoving = false;
            Ball.transform.position = ballStartPosition;
            ballRigidbody.velocity = Vector3.zero;
            ballRigidbody.angularVelocity = Vector3.zero;
            // Собираем информацию о кеглях
            int kegelsUp = 0;
            int kegelsDown = 0;
            foreach(GameObject kegel in
                GameObject.FindGameObjectsWithTag("Kegel"))
            {
                // ? чем отличаются стоящие и упавшие кегли
                // стоит - у==0, лежит - y==0.6 это примерно, 
                // будем считать, что у > 0.1 - упала
                if(kegel.transform.position.y > 0.1)
                {
                    kegel.SetActive(false);
                    kegelsDown++;
                } 
                else
                {
                    kegelsUp++;
                    // Если кегля недоупала (на стене или на другой) - выровняем ее
                    kegel.transform.rotation = Quaternion.Euler(0, 0, 0);
                    kegel.transform.position.Set(kegel.transform.position.x, 0, kegel.transform.position.z);
                }
                // Debug.Log(kegel.transform.position);
            }
            // Отображаем стрелку
            Arrow.SetActive(true);
            // Выводим статистику
            attempt++;
            GameStat.text += "\n" + attempt + "  " + Clock.StringValue + "  " +
                + kegelsDown + "  " + kegelsUp;
        }
        #endregion

        #region Запуск шарика
        if (Input.GetKeyDown(KeyCode.Space) && ! isBallMoving)
        {
            // Толкнуть шарик - приложить силу к его твердому телу
            // Направление силы - по стрелке
            Vector3 forceDirection = Arrow.transform.forward;
            // Величина силы - по индикатору (MIN - MAX)
            float forceFactor = MIN_FORCE + (MAX_FORCE - MIN_FORCE) * ForceIndicator.fillAmount;
            ballRigidbody.AddForce(forceFactor * forceDirection);
            ballRigidbody.velocity = forceDirection * 0.1f;
            isBallMoving = true;
            Arrow.SetActive(false);
        }
        // Задание: учесть величину ForceIndicator при расчете силы
        // Задание: прятать индикатор на время движения шарика 
        #endregion

        #region Вращение стрелки
        if (Input.GetKey(KeyCode.LeftArrow)
         && arrowAngle > -maxArrowAngle)
        {
            Arrow.transform.RotateAround(      // Вращение:
                ArrowTail.transform.position,  // Центр вращения
                Vector3.up,                    // Ось вращения
                -1f                            // Угол поворота
            );
            arrowAngle -= 1f;
        }
        if (Input.GetKey(KeyCode.RightArrow)
         && arrowAngle < maxArrowAngle)
        {
            Arrow.transform.RotateAround(     
                ArrowTail.transform.position, 
                Vector3.up,                   
                1f                             
            );
            arrowAngle += 1f;
        }
        #endregion

        #region Индикатор силы
        if (!isBallMoving)
        {
            if (Input.GetKey(KeyCode.UpArrow))
            {
                // ForceIndicator.fillAmount += 0.01f;  // ! time dependent !
                float val = ForceIndicator.fillAmount + Time.deltaTime / 2;
                if( val <= 1)
                    ForceIndicator.fillAmount = val;
            }
            if (Input.GetKey(KeyCode.DownArrow))
            {
                float val = ForceIndicator.fillAmount - Time.deltaTime / 2;
                if (val >= .1f)
                    ForceIndicator.fillAmount = val;
            }
        }
        // Задание: ограничить величину 0.1 <= fillAmount <= 1
        #endregion

    }

    /**
     * Обработчик кнопки Play меню
     * 
     */
    public void PlayClick()
    {
        // Debug.Log("Click");
        GameMenu.SetActive(false);
        Menu.IsActive = false;

    }

    // Задание: по нажатию на кнопку "В" отображать таблицу рекордов и
    // ставить игру на паузу (останавливать таймер). НО! не во время
    // движения шара. (на экране - создать доп. холст - Canvas)
    // + сортировать результаты: чем меньше шаров, тем лучше.
    //   если одинаково - чем меньше времени

    /**
     * Считываем таблицу лучших результатов.
     * Если ее нет - создаем тестовую
     */
    private void LoadBestResults()
    {
        // файл с результатами - обявлен в константах
        if (File.Exists(BEST_RES_FILE))
        {
            using (StreamReader reader = new StreamReader(BEST_RES_FILE))
            {
                XmlSerializer serializer = new XmlSerializer(
                    typeof(List<GameResult>));
                bestResults = (List<GameResult>)
                    serializer.Deserialize(reader);
            }
            bestResults.Sort();
            foreach(var res in bestResults)
            {
                Debug.Log(res);
            }
        }
        else
        {
            // файла нет - создаем тестовый 
            bestResults = new List<GameResult>();
            bestResults.Add(new GameResult { Balls = 20, Time = 200 });
            bestResults.Add(new GameResult { Balls = 30, Time = 300 });
            bestResults.Add(new GameResult { Balls = 10, Time = 100 });
            
            using(StreamWriter writer = new StreamWriter(BEST_RES_FILE))
            {
                XmlSerializer serializer = new XmlSerializer(
                    bestResults.GetType());
                serializer.Serialize(writer, bestResults);
            }
        }
    }
}
/*
 * Работа с сохранением - сериализация.
 * Сериализация - механизм представления обекта (параллельной формі) в виде
 * последовательности (серии), обічно, для сохранения в файле или
 * для передачи по сети.
 * Сохранение - віделение числовіх (не-ссілочніх) характеристик,
 * которіе могут біть сохранені и позже восставновленні.
 */
public class GameResult: System.IComparable<GameResult>
{
    public int Balls { get; set; }   // бросков
    public float Time { get; set; }  // время раунда


    public int CompareTo(GameResult y)
    {
        if (this.Balls < y.Balls) return -1;
        else if (this.Balls == y.Balls)
        {
            if (this.Time < y.Time) return -1;
            else if (this.Time == y.Time) return 0;
        }
        return 1;
    }

    public override string ToString()
    {
        return "Balls: " + Balls + ", Time: " + Time; 
    }
}


/*
 * Git:
 * git init - инициализировать локальн. репозиторий
 * git remote add origin http... - указать удаленку
 * git add . - добавить всю текущую директорию
 * git commit -m "commit message"  - создать "точку"-версию
 *   генерируем токен (github - settins - developer - personal access tokens)
 *   сохраняем на ПК, через гит его повторно посмотреть нельзя
 * git push --all
 */