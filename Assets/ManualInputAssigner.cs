using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

public class ManualInputAssigner : MonoBehaviour
{
    public PlayerInput player1;
    public PlayerInput player2;

    void OnEnable()
    {
        InputSystem.onDeviceChange += OnDeviceChange;
    }

    void OnDisable()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
    }

    void Start()
    {
        AsignarControles();
    }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        // Si se conecta o reconecta un mando, reasignamos todo
        if (change == InputDeviceChange.Added || change == InputDeviceChange.Reconnected)
        {
            Debug.Log($"Nuevo dispositivo detectado: {device.displayName}. Reasignando...");
            AsignarControles();
        }
    }

    public void AsignarControles()
    {
        //Limpiar emparejamientos actuales
        player1.user.UnpairDevices();
        player2.user.UnpairDevices();

        //Teclado para ambos
        if (Keyboard.current != null)
        {
            InputUser.PerformPairingWithDevice(Keyboard.current, player1.user);
            InputUser.PerformPairingWithDevice(Keyboard.current, player2.user);
        }

        //Mandos
        var gamepads = Gamepad.all;
        if (gamepads.Count > 0)
        {
            InputUser.PerformPairingWithDevice(gamepads[0], player1.user);
            Debug.Log("Mando 1 asignado al Jugador 1");
        }
        if (gamepads.Count > 1)
        {
            InputUser.PerformPairingWithDevice(gamepads[1], player2.user);
            Debug.Log("Mando 2 asignado al Jugador 2");
        }

        //Forzar esquemas de control
        player1.user.ActivateControlScheme("Teclado 1");
        player2.user.ActivateControlScheme("Teclado 2");

        player1.neverAutoSwitchControlSchemes = true;
        player2.neverAutoSwitchControlSchemes = true;
    }
}