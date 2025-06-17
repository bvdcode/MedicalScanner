#include <DHT.h>
#include <ArduinoBLE.h>
#include <Arduino_LED_Matrix.h>

constexpr uint8_t PIN_SPEAKER = 9;  // PWM pin
constexpr uint8_t PIN_DHT = 2;      // DATA
constexpr uint8_t DHT_TYPE = DHT11;

DHT dht(PIN_DHT, DHT_TYPE);
ArduinoLEDMatrix matrix;
BLEService tempService("1809");
BLEFloatCharacteristic tempChar("2A1C", BLERead | BLENotify);

const unsigned long WAIT_INTERVAL = 1000;  // ms
const unsigned long UI_PERIOD = 80;        // ms (animation frame)
unsigned long lastSensorMs = 0;
unsigned long lastUiMs = 0;

bool isConnected = false;
constexpr uint8_t ROWS = 8;
constexpr uint8_t COLS = 12;

const uint8_t idleFrames[][ROWS][COLS] PROGMEM = {
  { { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
    { 0, 0, 0, 0, 1, 1, 1, 1, 0, 0, 0, 0 },
    { 0, 0, 0, 1, 1, 0, 0, 1, 1, 0, 0, 0 },
    { 0, 0, 1, 1, 0, 0, 0, 0, 1, 1, 0, 0 },
    { 0, 0, 1, 1, 0, 0, 0, 0, 1, 1, 0, 0 },
    { 0, 0, 0, 1, 1, 0, 0, 1, 1, 0, 0, 0 },
    { 0, 0, 0, 0, 1, 1, 1, 1, 0, 0, 0, 0 },
    { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },

  { { 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0 },
    { 0, 0, 0, 1, 1, 0, 0, 1, 1, 0, 0, 0 },
    { 0, 0, 1, 1, 0, 0, 0, 0, 1, 1, 0, 0 },
    { 0, 1, 1, 0, 0, 0, 0, 0, 0, 1, 1, 0 },
    { 0, 1, 1, 0, 0, 0, 0, 0, 0, 1, 1, 0 },
    { 0, 0, 1, 1, 0, 0, 0, 0, 1, 1, 0, 0 },
    { 0, 0, 0, 1, 1, 0, 0, 1, 1, 0, 0, 0 },
    { 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0 } },

  { { 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0 },
    { 0, 0, 1, 1, 0, 0, 0, 0, 1, 1, 0, 0 },
    { 0, 1, 1, 0, 0, 0, 0, 0, 0, 1, 1, 0 },
    { 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1 },
    { 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1 },
    { 0, 1, 1, 0, 0, 0, 0, 0, 0, 1, 1, 0 },
    { 0, 0, 1, 1, 0, 0, 0, 0, 1, 1, 0, 0 },
    { 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0 } },

  { { 0, 0, 1, 1, 0, 0, 0, 0, 1, 1, 0, 0 },
    { 0, 1, 1, 0, 0, 0, 0, 0, 0, 1, 1, 0 },
    { 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1 },
    { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
    { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
    { 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1 },
    { 0, 1, 1, 0, 0, 0, 0, 0, 0, 1, 1, 0 },
    { 0, 0, 1, 1, 0, 0, 0, 0, 1, 1, 0, 0 } },

  { { 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0 },
    { 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1 },
    { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
    { 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0 },
    { 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0 },
    { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
    { 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1 },
    { 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0 } },

  { { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
    { 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0 },
    { 0, 0, 0, 0, 1, 1, 1, 1, 0, 0, 0, 0 },
    { 0, 0, 0, 1, 1, 0, 0, 1, 1, 0, 0, 0 },
    { 0, 0, 0, 1, 1, 0, 0, 1, 1, 0, 0, 0 },
    { 0, 0, 0, 0, 1, 1, 1, 1, 0, 0, 0, 0 },
    { 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0 },
    { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 } }
};

const uint8_t activeFrames[][ROWS][COLS] PROGMEM = {
  { { 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
    { 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
    { 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
    { 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
    { 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
    { 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
    { 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
    { 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },

  { { 0, 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0 },
    { 0, 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0 },
    { 0, 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0 },
    { 0, 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0 },
    { 0, 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0 },
    { 0, 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0 },
    { 0, 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0 },
    { 0, 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0 } },

  { { 0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0, 0 },
    { 0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0, 0 },
    { 0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0, 0 },
    { 0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0, 0 },
    { 0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0, 0 },
    { 0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0, 0 },
    { 0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0, 0 },
    { 0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0, 0 } },

  { { 0, 0, 0, 0, 0, 0, 1, 1, 1, 0, 0, 0 },
    { 0, 0, 0, 0, 0, 0, 1, 1, 1, 0, 0, 0 },
    { 0, 0, 0, 0, 0, 0, 1, 1, 1, 0, 0, 0 },
    { 0, 0, 0, 0, 0, 0, 1, 1, 1, 0, 0, 0 },
    { 0, 0, 0, 0, 0, 0, 1, 1, 1, 0, 0, 0 },
    { 0, 0, 0, 0, 0, 0, 1, 1, 1, 0, 0, 0 },
    { 0, 0, 0, 0, 0, 0, 1, 1, 1, 0, 0, 0 },
    { 0, 0, 0, 0, 0, 0, 1, 1, 1, 0, 0, 0 } },

  { { 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 0 },
    { 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 0 },
    { 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 0 },
    { 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 0 },
    { 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 0 },
    { 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 0 },
    { 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 0 },
    { 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 0 } },

  { { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1 },
    { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1 },
    { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1 },
    { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1 },
    { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1 },
    { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1 },
    { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1 },
    { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1 } }
};

void beep(uint8_t times = 1) {
  for (uint8_t i = 0; i < times; ++i) {
    tone(PIN_SPEAKER, 300, 80);
    delay(120);
  }
}

void setup() {
  Serial.begin(115200);
  while (!Serial) {
    delay(WAIT_INTERVAL);
  }
  Serial.println("Boot…");
  pinMode(PIN_SPEAKER, OUTPUT);
  beep();

  dht.begin();
  matrix.begin();

  if (!BLE.begin()) {
    Serial.println("BLE init failed");
    for (;;) {
      delay(WAIT_INTERVAL);
    }
  }
  BLE.setLocalName("Bodymeter Celsius");
  tempService.addCharacteristic(tempChar);
  BLE.addService(tempService);
  BLE.advertise();
  Serial.println("BLE advertising…");
}

void loop() {
  BLEDevice central = BLE.central();
  if (central && !isConnected) {
    isConnected = true;
    beep(2);
    Serial.print("Connected: ");
    Serial.println(central.address());
  }
  if (isConnected && !central.connected()) {
    isConnected = false;
    beep(3);
    Serial.println("Disconnected");
  }

  unsigned long now = millis();

  if (isConnected && now - lastSensorMs >= WAIT_INTERVAL) {
    lastSensorMs = now;
    float t = dht.readTemperature();  // °C
    if (!isnan(t)) {
      tempChar.writeValue(t);
      Serial.print("T=");
      Serial.println(t);
    }
  }

  if (now - lastUiMs >= UI_PERIOD) {
    lastUiMs = now;
    static uint8_t frameIdx = 0;

    if (isConnected) {
      matrix.loadPixels((uint8_t*)activeFrames[frameIdx], ROWS * COLS);
      frameIdx = (frameIdx + 1) % (sizeof(activeFrames) / sizeof(activeFrames[0]));
    } else {
      matrix.loadPixels((uint8_t*)idleFrames[frameIdx], ROWS * COLS);
      frameIdx = (frameIdx + 1) % (sizeof(idleFrames) / sizeof(idleFrames[0]));
    }
  }

  BLE.poll();
  delay(250);
}
