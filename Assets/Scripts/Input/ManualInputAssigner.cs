using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

public class ManualInputAssigner : MonoBehaviour
{
    [Header("Players")]
    public PlayerInput player1;
    public PlayerInput player2;

    private void OnEnable()
    {
        InputSystem.onDeviceChange += OnDeviceChange;
    }

    private void OnDisable()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
    }

    private void Start()
    {
        // Esperamos un frame para asegurarnos
        // de que PlayerInput haya creado su InputUser
        Invoke(nameof(AsignarControles), 0.1f);
    }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (change == InputDeviceChange.Added ||
            change == InputDeviceChange.Reconnected)
        {
            Debug.Log($"Dispositivo detectado: {device.displayName}");
            AsignarControles();
        }
    }

    public void AsignarControles()
    {
        if (player1 == null || player2 == null)
            return;

        // ---------- LIMPIAR SOLO SI ES VÁLIDO ----------
        ClearUser(player1);
        ClearUser(player2);

        // ---------- TECLADO ----------
        if (Keyboard.current != null)
        {
            PairDevice(player1, Keyboard.current);
            PairDevice(player2, Keyboard.current);
        }

        // ---------- GAMEPADS ----------
        var gamepads = Gamepad.all;

        if (gamepads.Count > 0)
        {
            PairDevice(player1, gamepads[0]);
            Debug.Log("Mando 1 ? Jugador 1");
        }

        if (gamepads.Count > 1)
        {
            PairDevice(player2, gamepads[1]);
            Debug.Log("Mando 2 ? Jugador 2");
        }

        // ---------- ESQUEMAS ----------
        ActivateScheme(player1, "Teclado 1");
        ActivateScheme(player2, "Teclado 2");

        player1.neverAutoSwitchControlSchemes = true;
        player2.neverAutoSwitchControlSchemes = true;
    }

    // ===================== HELPERS =====================

    private void ClearUser(PlayerInput player)
    {
        if (player.user.valid)
            player.user.UnpairDevices();
    }

    private void PairDevice(PlayerInput player, InputDevice device)
    {
        if (!player.user.valid)
            return;

        InputUser.PerformPairingWithDevice(device, player.user);
    }

    private void ActivateScheme(PlayerInput player, string scheme)
    {
        if (!player.user.valid)
            return;

        player.user.ActivateControlScheme(scheme);
    }
}
