using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using System;
using System.Collections.Generic;

namespace Origin.Source.IO
{
    public static class InputManager
    {
        #region Variables

        private static KeyboardState CurrentKeys;
        private static KeyboardState PreviousKeys;

        private static MouseState CurrentMouse;
        private static MouseState PreviousMouse;

        private static GamePadState CurrentGamepad;
        private static GamePadState PreviousGamepad;

        #region Misc Mouse Properties

        public static int MouseX
        {
            get
            {
                return CurrentMouse.X;
            }
        }

        public static int MouseY
        {
            get
            {
                return CurrentMouse.Y;
            }
        }

        public static int MouseDeltaX
        {
            get
            {
                return PreviousMouse.X - CurrentMouse.X;
            }
        }

        public static int MouseDeltaY
        {
            get
            {
                return PreviousMouse.Y - CurrentMouse.Y;
            }
        }

        public static int MouseScrollNotchesY
        {
            get
            {
                return CurrentMouse.ScrollWheelValue - PreviousMouse.ScrollWheelValue;
            }
        }

        public static int MouseScrollNotchesX
        {
            get
            {
                return CurrentMouse.HorizontalScrollWheelValue - PreviousMouse.HorizontalScrollWheelValue;
            }
        }

        public static int MouseScrollWheel
        {
            get
            {
                return CurrentMouse.ScrollWheelValue - PreviousMouse.ScrollWheelValue;
            }
        }

        #endregion Misc Mouse Properties

        /// <summary>
        /// The Gamepad port to use
        /// </summary>
        public static PlayerIndex GamepadPort = PlayerIndex.One;

        /// <summary>
        /// Whether a gamepad is connected and recognised by the game
        /// </summary>
        public static bool IsGamepadConnected = false;

        /// <summary>
        /// Whether to use gamepad for input
        /// </summary>
        public static bool UseGamepad = false;

        /// <summary>
        /// Game object
        /// </summary>
        public static Game game;

        private static Dictionary<string, Keybind> keybinds = new Dictionary<string, Keybind>();
        private static bool _centerMouse = false;

        #endregion Variables

        #region Initialisation

        /// <summary>
        /// Registers keybinds
        /// </summary>
        public static void Initialise(Game game)
        {
            //Camera
            BindKey("camera.up", new Keybind(Keys.W));
            BindKey("camera.down", new Keybind(Keys.S));
            BindKey("camera.left", new Keybind(Keys.A));
            BindKey("camera.right", new Keybind(Keys.D));
            BindKey("camera.zoom.plus", new Keybind(Keys.OemPlus));
            BindKey("camera.zoom.minus", new Keybind(Keys.OemMinus));

            //World
            BindKey("world.level.plus", new Keybind(Keys.OemCloseBrackets, 300, 50));
            BindKey("world.level.minus", new Keybind(Keys.OemOpenBrackets, 300, 50));

            //Game
            BindKey("game.halfwallswitch", new Keybind(Keys.F));
            BindKey("game.exit", new Keybind(Keys.Escape));

            // Manual control
            BindKey("manual.tr", new Keybind(Keys.W, 300, 100));
            BindKey("manual.tl", new Keybind(Keys.A, 300, 100));
            BindKey("manual.br", new Keybind(Keys.D, 300, 100));
            BindKey("manual.bl", new Keybind(Keys.S, 300, 100));

            BindKey("ctrl", new Keybind(Keys.LeftControl));
            BindKey("shift", new Keybind(Keys.LeftShift));

            BindKey("mouse.left", new Keybind(MouseButton.Button0, Buttons.None));
            BindKey("mouse.right", new Keybind(MouseButton.Button1, Buttons.None));
            BindKey("mouse.middle", new Keybind(MouseButton.Button2, Buttons.None));

            BindKey("num.1", new Keybind(Keys.D1));
            BindKey("num.2", new Keybind(Keys.D2));
            BindKey("num.3", new Keybind(Keys.D3));
            BindKey("num.4", new Keybind(Keys.D4));
            BindKey("num.5", new Keybind(Keys.D5));
        }

        private static void BindKey(string name, Keybind fallback)
        {
            if (!keybinds.ContainsKey(name.ToLower()))
            {
                //try to load the keybind from file
                keybinds.Add(name.ToLower(), fallback);
            }
            else
            {
                Console.Error.WriteLine("An error occured when registering keybind \"" + name.ToLower() + "\"! (ERR_KEYBIND_CONFLICT)");
            }
        }

        /// <summary>
        /// Rebinds a key to the given Keybind
        /// </summary>
        /// <param name="name"></param>
        /// <param name="newBinding"></param>
        public static void RebindKey(string name, Keybind newBinding)
        {
            if (keybinds.ContainsKey(name.ToLower()))
            {
                Keybind toBind = keybinds[name.ToLower()];
                toBind.keyboardBinding = newBinding.keyboardBinding;
                toBind.gamepadBinding = newBinding.gamepadBinding;
                toBind.mouseBinding = newBinding.mouseBinding;
                keybinds[name.ToLower()] = toBind;
            }
        }

        /// <summary>
        /// Returns a keybind representing the currently pressed key
        /// </summary>
        /// <returns></returns>
        public static Keybind CaptureBinding()
        {
            Keybind currentBinding = null;

            //Get the first element in the current keyboard keys
            currentBinding.keyboardBinding = CurrentKeys.GetPressedKeys()[0];
            //Get the first element in the current gamepad state
            currentBinding.gamepadBinding = GetGamepadButtons();
            //Get the first element in the current mouse state
            currentBinding.mouseBinding = GetMouseButtons();

            return currentBinding;
        }

        #region Current getters

        private static Buttons GetGamepadButtons()
        {
            Buttons buttons = new Buttons();

            //DPad
            if (CurrentGamepad.IsButtonDown(Buttons.DPadUp))
            {
                buttons = Buttons.DPadUp;
            }
            else if (CurrentGamepad.IsButtonDown(Buttons.DPadDown))
            {
                buttons = Buttons.DPadDown;
            }
            else if (CurrentGamepad.IsButtonDown(Buttons.DPadLeft))
            {
                buttons = Buttons.DPadLeft;
            }
            else if (CurrentGamepad.IsButtonDown(Buttons.DPadRight))
            {
                buttons = Buttons.DPadRight;
            }

            //Start and Back
            else if (CurrentGamepad.IsButtonDown(Buttons.Start))
            {
                buttons = Buttons.Start;
            }
            else if (CurrentGamepad.IsButtonDown(Buttons.Back))
            {
                buttons = Buttons.Back;
            }

            //Shoulders and Triggers
            else if (CurrentGamepad.IsButtonDown(Buttons.LeftShoulder))
            {
                buttons = Buttons.LeftShoulder;
            }
            else if (CurrentGamepad.IsButtonDown(Buttons.RightShoulder))
            {
                buttons = Buttons.RightShoulder;
            }
            else if (CurrentGamepad.IsButtonDown(Buttons.RightTrigger))
            {
                buttons = Buttons.RightTrigger;
            }
            else if (CurrentGamepad.IsButtonDown(Buttons.LeftTrigger))
            {
                buttons = Buttons.LeftTrigger;
            }

            //Big button
            else if (CurrentGamepad.IsButtonDown(Buttons.BigButton))
            {
                buttons = Buttons.BigButton;
            }

            //Action buttons
            else if (CurrentGamepad.IsButtonDown(Buttons.A))
            {
                buttons = Buttons.A;
            }
            else if (CurrentGamepad.IsButtonDown(Buttons.B))
            {
                buttons = Buttons.B;
            }
            else if (CurrentGamepad.IsButtonDown(Buttons.X))
            {
                buttons = Buttons.X;
            }
            else if (CurrentGamepad.IsButtonDown(Buttons.Y))
            {
                buttons = Buttons.Y;
            }

            //Stick Clicks
            else if (CurrentGamepad.IsButtonDown(Buttons.LeftStick))
            {
                buttons = Buttons.LeftStick;
            }
            else if (CurrentGamepad.IsButtonDown(Buttons.RightStick))
            {
                buttons = Buttons.RightStick;
            }

            //Right thumbstick
            else if (CurrentGamepad.IsButtonDown(Buttons.RightThumbstickUp))
            {
                buttons = Buttons.RightThumbstickUp;
            }
            else if (CurrentGamepad.IsButtonDown(Buttons.RightThumbstickDown))
            {
                buttons = Buttons.RightThumbstickDown;
            }
            else if (CurrentGamepad.IsButtonDown(Buttons.RightThumbstickRight))
            {
                buttons = Buttons.RightThumbstickRight;
            }
            else if (CurrentGamepad.IsButtonDown(Buttons.RightThumbstickLeft))
            {
                buttons = Buttons.RightThumbstickLeft;
            }

            //Left thumbstick
            else if (CurrentGamepad.IsButtonDown(Buttons.LeftThumbstickUp))
            {
                buttons = Buttons.LeftThumbstickUp;
            }
            else if (CurrentGamepad.IsButtonDown(Buttons.LeftThumbstickDown))
            {
                buttons = Buttons.LeftThumbstickDown;
            }
            else if (CurrentGamepad.IsButtonDown(Buttons.LeftThumbstickRight))
            {
                buttons = Buttons.LeftThumbstickRight;
            }
            else if (CurrentGamepad.IsButtonDown(Buttons.LeftThumbstickLeft))
            {
                buttons = Buttons.LeftThumbstickLeft;
            }

            return buttons;
        }

        private static MouseButton GetMouseButtons()
        {
            MouseButton mouse = new MouseButton();

            //Check the button
            if (CurrentMouse.LeftButton == ButtonState.Pressed)
            {
                mouse = MouseButton.Button0;
            }
            else if (CurrentMouse.RightButton == ButtonState.Pressed)
            {
                mouse = MouseButton.Button1;
            }
            else if (CurrentMouse.MiddleButton == ButtonState.Pressed)
            {
                mouse = MouseButton.Button2;
            }
            else if (CurrentMouse.XButton1 == ButtonState.Pressed)
            {
                mouse = MouseButton.Button3;
            }
            else if (CurrentMouse.XButton2 == ButtonState.Pressed)
            {
                mouse = MouseButton.Button4;
            }

            return mouse;
        }

        #endregion Current getters

        #endregion Initialisation

        #region Input Handling

        /// <summary>
        /// Centers the mouse
        /// </summary>
        public static void CenterMouse()
        {
            _centerMouse = true;
        }

        /// <summary>
        /// Returns whether the keybind is currently pressed.
        /// </summary>
        public static bool IsPressed(string name)
        {
            if (keybinds.ContainsKey(name.ToLower()))
            {
                return IsKeybindPressedCurrent(keybinds[name.ToLower()]);
            }

            Console.Error.WriteLine("Keybind \"" + name.ToLower() + "\" doesn't exist! (ERR_KEYBIND_NULL)");

            return false;
        }

        /// <summary>
        /// Returns whether the keybind was released
        /// </summary>
        public static bool JustReleased(string name)
        {
            if (keybinds.ContainsKey(name.ToLower()))
            {
                return IsKeybindPressedCurrent(keybinds[name.ToLower()]) == false && IsKeybindPressedPrevious(keybinds[name.ToLower()]) == true;
            }

            Console.Error.WriteLine("Keybind \"" + name.ToLower() + "\" doesn't exist! (ERR_KEYBIND_NULL)");

            return false;
        }

        /// <summary>
        /// Returns whether the keybind started being pressed
        /// </summary>
        public static bool JustPressed(string name)
        {
            if (keybinds.ContainsKey(name.ToLower()))
            {
                bool res = IsKeybindPressedCurrent(keybinds[name.ToLower()]) == true && IsKeybindPressedPrevious(keybinds[name.ToLower()]) == false;
                return res;
            }

            Console.Error.WriteLine("Keybind \"" + name.ToLower() + "\" doesn't exist! (ERR_KEYBIND_NULL)");

            return false;
        }

        public static bool JustPressedAndHoldDelayed(string name)
        {
            if (keybinds.ContainsKey(name.ToLower()))
            {
                bool justPressed = IsKeybindPressedCurrent(keybinds[name.ToLower()]) == true && IsKeybindPressedPrevious(keybinds[name.ToLower()]) == false;
                if (justPressed)
                {
                    keybinds[name.ToLower()].SetInitialDelay();
                    return true;
                }
                bool isPressed = IsKeybindPressedCurrent(keybinds[name.ToLower()]) && keybinds[name.ToLower()].delayTimer <= 0;
                if (isPressed)
                {
                    keybinds[name.ToLower()].SetRepeatDelay();
                    return true;
                }
                return false;
            }

            Console.Error.WriteLine("Keybind \"" + name.ToLower() + "\" doesn't exist! (ERR_KEYBIND_NULL)");

            return false;
        }

        #endregion Input Handling

        #region Keybind Checks

        private static bool IsKeybindPressedCurrent(Keybind keybind)
        {
            bool isPressed = false;
            //if (IsGamepadConnected && UseGamepad)
            if (UseGamepad)
            {
                isPressed = CurrentGamepad.IsButtonDown(keybind.gamepadBinding);
            }
            if (!keybind.GamepadOnly)
            {
                if (!keybind.PreferKeyboard)
                {
                    //Mouse keybinds
                    isPressed = isPressed || keybind.mouseBinding == MouseButton.Button0 && CurrentMouse.LeftButton == ButtonState.Pressed;
                    isPressed = isPressed || keybind.mouseBinding == MouseButton.Button1 && CurrentMouse.RightButton == ButtonState.Pressed;
                    isPressed = isPressed || keybind.mouseBinding == MouseButton.Button2 && CurrentMouse.MiddleButton == ButtonState.Pressed;

                    isPressed = isPressed || keybind.mouseBinding == MouseButton.Button3 && CurrentMouse.XButton1 == ButtonState.Pressed;
                    isPressed = isPressed || keybind.mouseBinding == MouseButton.Button4 && CurrentMouse.XButton2 == ButtonState.Pressed;
                }

                //Keyboard binding
                isPressed = isPressed || CurrentKeys.IsKeyDown(keybind.keyboardBinding);
            }
            return isPressed;
        }

        private static bool IsKeybindPressedPrevious(Keybind keybind)
        {
            bool isPressed = false;
            //if (IsGamepadConnected && UseGamepad)
            if (UseGamepad)
            {
                isPressed = PreviousGamepad.IsButtonDown(keybind.gamepadBinding);
            }
            if (!keybind.GamepadOnly)
            {
                if (!keybind.PreferKeyboard)
                {
                    //Mouse keybinds
                    isPressed = isPressed || keybind.mouseBinding == MouseButton.Button0 && PreviousMouse.LeftButton == ButtonState.Pressed;
                    isPressed = isPressed || keybind.mouseBinding == MouseButton.Button1 && PreviousMouse.RightButton == ButtonState.Pressed;
                    isPressed = isPressed || keybind.mouseBinding == MouseButton.Button2 && PreviousMouse.MiddleButton == ButtonState.Pressed;

                    isPressed = isPressed || keybind.mouseBinding == MouseButton.Button3 && PreviousMouse.XButton1 == ButtonState.Pressed;
                    isPressed = isPressed || keybind.mouseBinding == MouseButton.Button4 && PreviousMouse.XButton2 == ButtonState.Pressed;
                }

                //Keyboard binding
                isPressed = isPressed || PreviousKeys.IsKeyDown(keybind.keyboardBinding);
            }
            return isPressed;
        }

        #endregion Keybind Checks

        #region Update

        public static void Update(GameTime gameTime)
        {
            //Set the current state
            CurrentKeys = Keyboard.GetState();
            CurrentMouse = Mouse.GetState();

            foreach (var kbkey in keybinds.Keys)
            {
                keybinds[kbkey].TickTimer(gameTime);
            }
        }

        public static void FinalUpdate()
        {
            if (_centerMouse)
            {
                Mouse.SetPosition(game.GraphicsDevice.PresentationParameters.BackBufferWidth / 2, game.GraphicsDevice.PresentationParameters.BackBufferHeight / 2);
                _centerMouse = false;
            }

            //Save the current state
            PreviousKeys = CurrentKeys;
            PreviousMouse = Mouse.GetState();

            //Gamepad handling
            for (PlayerIndex i = PlayerIndex.One; i <= PlayerIndex.Four; i++)
            {
                GamePadCapabilities state = GamePad.GetCapabilities(i);
                if (state.IsConnected)
                {
                    GamepadPort = i;
                }
            }
            GamePadCapabilities capabilities = GamePad.GetCapabilities(GamepadPort);
            IsGamepadConnected = capabilities.IsConnected;
            //if (IsGamepadConnected)
            {
                PreviousGamepad = CurrentGamepad;
                CurrentGamepad = GamePad.GetState(GamepadPort);
                UseGamepad = UseGamepad || CurrentGamepad.IsButtonDown(
                    //If any button is down currently
                    Buttons.DPadUp | Buttons.DPadDown | Buttons.DPadLeft | Buttons.DPadRight |
                    Buttons.Start | Buttons.Back | Buttons.LeftStick | Buttons.RightStick |
                    Buttons.LeftShoulder | Buttons.RightShoulder | Buttons.BigButton |
                    Buttons.A | Buttons.B | Buttons.X | Buttons.Y |
                    Buttons.RightTrigger | Buttons.LeftTrigger |
                    Buttons.RightThumbstickUp | Buttons.RightThumbstickDown | Buttons.RightThumbstickRight | Buttons.RightThumbstickLeft |
                    Buttons.LeftThumbstickLeft | Buttons.LeftThumbstickUp | Buttons.LeftThumbstickDown | Buttons.LeftThumbstickRight);
            }
        }

        #endregion Update

        #region Helpers

        ///<summary>
        ///Currently only used for debugging purposes.
        ///</summary>
        public static void InspectGamePad(int playerNum)
        {
            GamePadCapabilities gpc = GamePad.GetCapabilities(playerNum);

            System.Diagnostics.Debug.WriteLine("inspecting gamepad #" + playerNum);
            System.Diagnostics.Debug.WriteLine("\t type: " + gpc.GamePadType);

            System.Diagnostics.Debug.WriteLine("\t has left X joystick: " + gpc.HasLeftXThumbStick);
            System.Diagnostics.Debug.WriteLine("\t has left Y joystick: " + gpc.HasLeftYThumbStick);

            System.Diagnostics.Debug.WriteLine("\t has A button: " + gpc.HasAButton);
            System.Diagnostics.Debug.WriteLine("\t has B button: " + gpc.HasBButton);
            System.Diagnostics.Debug.WriteLine("\t has X button: " + gpc.HasXButton);
            System.Diagnostics.Debug.WriteLine("\t has Y button: " + gpc.HasYButton);

            System.Diagnostics.Debug.WriteLine("\t has back button: " + gpc.HasBackButton);
            System.Diagnostics.Debug.WriteLine("\t has big button: " + gpc.HasBigButton);
            System.Diagnostics.Debug.WriteLine("\t has start button: " + gpc.HasStartButton);

            System.Diagnostics.Debug.WriteLine("\t has Dpad Down button: " + gpc.HasDPadDownButton);
            System.Diagnostics.Debug.WriteLine("\t has Dpad Left button: " + gpc.HasDPadLeftButton);
            System.Diagnostics.Debug.WriteLine("\t has Dpad Right button: " + gpc.HasDPadRightButton);
            System.Diagnostics.Debug.WriteLine("\t has Dpad Up button: " + gpc.HasDPadUpButton);

            System.Diagnostics.Debug.WriteLine("\t has Left Shoulder button: " + gpc.HasLeftShoulderButton);
            System.Diagnostics.Debug.WriteLine("\t has Left Trigger button: " + gpc.HasLeftTrigger);
            System.Diagnostics.Debug.WriteLine("\t has Left Stick button: " + gpc.HasLeftStickButton);
            System.Diagnostics.Debug.WriteLine("\t has Left vibration motor: " + gpc.HasLeftVibrationMotor);

            System.Diagnostics.Debug.WriteLine("\t has Right Shoulder button: " + gpc.HasRightShoulderButton);
            System.Diagnostics.Debug.WriteLine("\t has Right Trigger button: " + gpc.HasRightTrigger);
            System.Diagnostics.Debug.WriteLine("\t has Right Stick button: " + gpc.HasRightStickButton);
            System.Diagnostics.Debug.WriteLine("\t has Right vibration motor: " + gpc.HasRightVibrationMotor);
        }

        #endregion Helpers
    }

    #region Enums

    public class Keybind
    {
        public Keys keyboardBinding;
        public MouseButton mouseBinding;
        public Buttons gamepadBinding;

        public int initialDelay = 0;
        public int repeatDelay = 0;
        public int delayTimer = 0;

        /// <summary>
        /// Whether to use keyboard or mouse on this keybind
        /// </summary>
        public bool PreferKeyboard;

        /// <summary>
        /// Whether this keybind only applies on gamepads
        /// </summary>
        public bool GamepadOnly;

        #region Constructors

        public Keybind(Keys key)
        {
            keyboardBinding = key;
            mouseBinding = new MouseButton();
            gamepadBinding = new Buttons();

            GamepadOnly = false;
            PreferKeyboard = true;
        }

        public Keybind(Keys key, ushort initDelay, ushort repDelay)
        {
            keyboardBinding = key;
            mouseBinding = new MouseButton();
            gamepadBinding = new Buttons();

            initialDelay = initDelay;
            repeatDelay = repDelay;

            GamepadOnly = false;
            PreferKeyboard = true;
        }

        public Keybind(Keys key, Buttons gamepad)
        {
            keyboardBinding = key;
            mouseBinding = new MouseButton();
            gamepadBinding = gamepad;

            GamepadOnly = false;
            PreferKeyboard = true;
        }

        public Keybind(Buttons gamepad)
        {
            keyboardBinding = new Keys();
            mouseBinding = new MouseButton();
            gamepadBinding = gamepad;

            GamepadOnly = true;
            PreferKeyboard = false;
        }

        public Keybind(MouseButton mouse, Buttons gamepad)
        {
            keyboardBinding = new Keys();
            mouseBinding = mouse;
            gamepadBinding = gamepad;

            GamepadOnly = false;
            PreferKeyboard = false;
        }

        public Keybind(Keys keyboard, MouseButton mouse, Buttons gamepad)
        {
            keyboardBinding = keyboard;
            mouseBinding = mouse;
            gamepadBinding = gamepad;

            GamepadOnly = false;
            PreferKeyboard = false;
        }

        #endregion Constructors

        #region Methods

        public void TickTimer(GameTime gameTime)
        {
            if (delayTimer > 0) delayTimer -= gameTime.ElapsedGameTime.Milliseconds;
        }

        public void SetInitialDelay()
        {
            delayTimer = initialDelay;
        }

        public void SetRepeatDelay()
        {
            delayTimer = repeatDelay;
        }

        #endregion Methods
    }

    /// <summary>
    /// Defines a mouse button
    /// </summary>
    public enum MouseButton
    {
        /// <summary>
        /// Left Mouse Button
        /// </summary>
        Button0,

        /// <summary>
        /// Right Mouse Button
        /// </summary>
        Button1,

        /// <summary>
        /// Middle Mouse Button
        /// </summary>
        Button2,

        /// <summary>
        /// Macro 1
        /// </summary>
        Button3,

        /// <summary>
        /// Macro 2
        /// </summary>
        Button4,
    }

    #endregion Enums
}