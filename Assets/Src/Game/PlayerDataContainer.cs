using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data container for player and related gui rendering
/// </summary>
public class PlayerDataContainer : MonoBehaviour
{
    // player reference
    public Player Player = null;

    // camera reference
    public PlayerCamera Camera = null;

    // camera look at reference
    public GameObject CameraFollow = null;

    // current score
    public int Score = 0;

    // max health and current health
    public const int MaxHealth = 100;
    public int Health = MaxHealth;

    private void Start()
    {
        Player.DataContainer = this;
        Camera.DataContainer = this;
    }

    /// <summary>
    /// Render gui
    /// </summary>
    private void OnGUI()
    {
        // if terrain manager editor is active, don't render this gui
        if (GameManager.Instance.TerrainManager.RenderGUI)
            return;

        GUIStyle style = GUI.skin.GetStyle("label");
        style.fontSize = 30;

        // score
        GUI.Label(new Rect(10.0f, 50.0f, 200.0f, 100.0f), "Score: " + Score);

        // time left
        GUI.Label(new Rect(Screen.width - 230.0f, 0.0f, 200.0f, 50.0f), "Time Left: " + System.TimeSpan.FromSeconds((int)GameManager.Instance.TimeLeft).ToString(@"m\:ss"));

        // health
        GUI.Label(new Rect(10.0f, 0.0f, 250.0f, 100.0f), "Health: " + Health);

        // controls
        GUI.Label(new Rect(10.0f, Screen.height - 40.0f, Screen.width-10.0f, 50.0f), "WASD to move. LEFT MOUSE BUTTON to shoot. SPACEBAR to change weapon. F1 to Enter/Exit edit mode.");

        // bullet type
        GUI.Label(new Rect(10.0f, 100.0f, 500.0f, 1000.0f), "Current Weapon: " + Player.CurrentBulletTypeDisplayString);

        // end game
        if(GameManager.Instance.TimeLeft<=0.0f || Health < 1)
        {
            style.fontSize = 150;
            GUI.Label(new Rect(Screen.width * 0.5f - 340.0f, Screen.height * 0.2f, Screen.width, Screen.height), Health > 0 ? "TIMES UP!" : "YOU DIED!");

            style.fontSize = 100;
            GUI.Label(new Rect(Screen.width * 0.5f - 200.0f, Screen.height * 0.42f, Screen.width, Screen.height), "SCORE: " + Score);

            style.fontSize = 70;
            GUI.Label(new Rect(Screen.width * 0.5f - 580.0f, Screen.height * 0.6f, Screen.width, Screen.height), "PRESS RETURN BUTTON TO RESTART");
        }
        else if(GameManager.Instance.HasDifficultyChange)
        {
            style.fontSize = 130;
            string text = "DIFFICULTY LEVEL UP!";
            float w = GUI.skin.label.CalcSize(new GUIContent(text)).x;
            GUI.Label(new Rect(Screen.width * 0.5f - w*0.5f, Screen.height * 0.4f, Screen.width, Screen.height), text);

        }
    }
}
