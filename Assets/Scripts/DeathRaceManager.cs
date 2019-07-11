using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class DeathRaceManager : NetworkBehaviour
{
    static List<CarMovement> cars = new List<CarMovement>();
    static List<CarMovement> allCars = new List<CarMovement>();

    private void Awake()
    {
        cars.Clear();
        allCars.Clear();
    }

    public static void ClearLists()
    {
        cars.Clear();
        allCars.Clear();
    }

    public static List<CarMovement> GetCars()
    {
        return allCars;
    }

    public static int NumberOfPlayers()
    {
        return allCars.Count;
    }

    public static CarMovement GetCarAtIndex(int index)
    {
        return allCars[index];
    }

    public static void AddCar(CarMovement car)
    {
        cars.Add(car);
        allCars.Add(car);
    }

    public static bool RemoveCarAndCheckWinner(CarMovement car)
    {
        cars.Remove(car);

        if (cars.Count == 1)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public static CarMovement GetWinnerByKill()
    {
        if (cars.Count == 1)
        {
            return cars[0];
        }
        return null;
    }

    public static List<CarMovement> GetWinner()
    {
        List<CarMovement> winners = new List<CarMovement>();
        if (cars.Count == 1)
        {
            winners.Add(cars[0]);
            return winners;
        }
        else if (IsAnyCarFinished())
        {
            foreach (CarMovement car in cars)
            {
                if (car.IsRaceFinished())
                    winners.Add(car);
            }
            return winners;
        }
        else
        {
            return null;
        }
    }

    public static bool IsAnyCarFinished()
    {
        foreach (CarMovement car in allCars)
        {
            if (car.IsRaceFinished())
                return true;
        }
        return false;
    }
}
