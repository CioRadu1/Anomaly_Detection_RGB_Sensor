#include <Wire.h>
#include "Adafruit_TCS34725.h"
#include <math.h>

#define BUFFER_SIZE 10
#define THRESHOLD 2.0

Adafruit_TCS34725 tcs = Adafruit_TCS34725(TCS34725_INTEGRATIONTIME_50MS, TCS34725_GAIN_4X);

float redBuffer[BUFFER_SIZE];
float greenBuffer[BUFFER_SIZE];
float blueBuffer[BUFFER_SIZE];
float lightBuffer[BUFFER_SIZE];

int bufferIndex = 0;
int msSum = 0;
int wfdSum = 0;
int orgSum = 0;
int bufferCount = 0;
int ten = 10;
int defect = 10;
float redCUSUM = 0, greenCUSUM = 0, blueCUSUM = 0, lightCUSUM = 0;
float redMean = 0, greenMean = 0, blueMean = 0, lightMean = 0;
float redM2 = 0, greenM2 = 0, blueM2 = 0, lightM2 = 0;
float redStdDev = 0, greenStdDev = 0, blueStdDev = 0, lightStdDev = 0;
float redZScore, greenZScore, blueZScore, lightZScore, redZScoreWelford, greenZScoreWelford, blueZScoreWelford, lightZScoreWelford;

float redCalibration = 0, greenCalibration = 0, blueCalibration = 0;
bool calibrated = false;

void GetColorData() {
  uint16_t r, g, b, c;
  tcs.getRawData(&r, &g, &b, &c);

  float sum = c;
  float red = (r / sum) * 255;
  float green = (g / sum) * 255;
  float blue = (b / sum) * 255;
  float light = read_adc(0);

  // Store values in buffer
  redBuffer[bufferIndex] = red;
  greenBuffer[bufferIndex] = green;
  blueBuffer[bufferIndex] = blue;
  lightBuffer[bufferIndex] = light;

  bufferIndex = (bufferIndex + 1) % BUFFER_SIZE;

  if (bufferCount < BUFFER_SIZE) bufferCount++;

  //Original
  updateBufferMeanAndVariance(red, redMean, redStdDev, redBuffer, bufferCount);
  updateBufferMeanAndVariance(green, greenMean, greenStdDev, greenBuffer, bufferCount);
  updateBufferMeanAndVariance(blue, blueMean, blueStdDev, blueBuffer, bufferCount);
  updateBufferMeanAndVariance(light, lightMean, lightStdDev, lightBuffer, bufferCount);

  //Welford's algorithm
  updateMeanAndVariance(red, redMean, redM2, bufferCount);
  updateMeanAndVariance(green, greenMean, greenM2, bufferCount);
  updateMeanAndVariance(blue, blueMean, blueM2, bufferCount);
  updateMeanAndVariance(light, lightMean, lightM2, bufferCount);
}

void updateBufferMeanAndVariance(float newValue, float &mean, float &stdDev, float buffer[], int count) {
  float sum = 0;
  for (int i = 0; i < count; i++) {
    sum += buffer[i];
  }
  mean = sum / count;

  float variance = 0;
  for (int i = 0; i < count; i++) {
    variance += pow(buffer[i] - mean, 2);
  }
  stdDev = sqrt(variance / count);
}

void updateMeanAndVariance(float newValue, float &mean, float &M2, int count) {
  float delta = newValue - mean;
  mean += delta / count;
  M2 += delta * (newValue - mean);
}

float calculateCUSUM(float value, float mean, float threshold, float& cusum) {
  cusum = max(0.0f, cusum + (value - mean - threshold));
  return cusum;
}

float calculateZScore(float value, float mean, float stdDev) {
  return (value - mean) / stdDev;
}

uint16_t read_adc(uint8_t channel) {
  ADMUX &= 0xE0;
  ADMUX |= channel&0x07;
  ADCSRB = channel&(1<<3);
  ADCSRA |= (1<<ADSC);
  while(ADCSRA & (1<<ADSC));
  return ADCW;
} 

ISR(TIMER1_COMPA_vect) {
  lightBuffer[bufferIndex] = read_adc(0);
}

void setup() {
  TCCR1A = 0;
  TCCR1B = 0;
  OCR1A = 7812;
  TCCR1B |= (1 << WGM12);
  TCCR1B |= (1 << CS12) | (1 << CS10);
  TIMSK1 |= (1 << OCIE1A);

  ADMUX = (1 << REFS0);
  ADCSRA = (1 << ADEN) | (1 << ADPS2) | (1 << ADPS1) | (1 << ADPS0);

  sei();
  Serial.begin(9600);
}

extern int __heap_start, *__brkval;

int usedStackSpace() {
  int stackPointer;
  return (int)&stackPointer - (__brkval == 0 ? (int)&__heap_start : (int)__brkval);
}

void loop() {
  GetColorData();

  if (defect != 0) {
    defect--;
    return;
  }
  unsigned long startTime, endTime, startTimeWelford, endTimeWelford, startTimeOriginal, endTimeOriginal;
  startTime = millis();

  float redValue = redBuffer[bufferIndex];
  float greenValue = greenBuffer[bufferIndex];
  float blueValue = blueBuffer[bufferIndex];
  float lightValue = lightBuffer[bufferIndex];

  startTimeOriginal = micros();

  redZScore = calculateZScore(redValue, redMean, redStdDev);
  greenZScore = calculateZScore(greenValue, greenMean, greenStdDev);
  blueZScore = calculateZScore(blueValue, blueMean, blueStdDev);
  lightZScore = calculateZScore(lightValue, lightMean, lightStdDev);

  endTimeOriginal = micros();
  orgSum += (endTimeOriginal - startTimeOriginal);

  startTimeWelford = micros();
  redZScoreWelford = calculateZScore(redValue, redMean, sqrt(redM2 / bufferCount));
  greenZScoreWelford = calculateZScore(greenValue, greenMean, sqrt(greenM2 / bufferCount));
  blueZScoreWelford = calculateZScore(blueValue, blueMean, sqrt(blueM2 / bufferCount));
  lightZScoreWelford = calculateZScore(lightValue, lightMean, sqrt(lightM2 / bufferCount));
  endTimeWelford = micros();
  wfdSum += (endTimeWelford - startTimeWelford);

  redCUSUM = calculateCUSUM(redValue, redMean, THRESHOLD, redCUSUM);
  greenCUSUM = calculateCUSUM(greenValue, greenMean, THRESHOLD, greenCUSUM);
  blueCUSUM = calculateCUSUM(blueValue, blueMean, THRESHOLD, blueCUSUM);
  lightCUSUM = calculateCUSUM(lightValue, lightMean, THRESHOLD, lightCUSUM);

  if (isnan(redZScore)) redZScore = 0;
  if (isnan(greenZScore)) greenZScore = 0;
  if (isnan(blueZScore)) blueZScore = 0;
  if (isnan(lightZScore)) lightZScore = 0;
  if (isnan(redZScoreWelford)) redZScoreWelford = 0;
  if (isnan(greenZScoreWelford)) greenZScoreWelford = 0;
  if (isnan(blueZScoreWelford)) blueZScoreWelford = 0;
  if (isnan(lightZScoreWelford)) lightZScoreWelford = 0;

  endTime = millis();
  msSum += (endTime - startTime);
  Serial.print(redValue, 2); Serial.print(",");
  Serial.print(greenValue, 2); Serial.print(",");
  Serial.print(blueValue, 2); Serial.print(",");
  Serial.print(lightValue, 2); Serial.print(",");
  Serial.print(redZScore, 2); Serial.print(",");
  Serial.print(greenZScore, 2); Serial.print(",");
  Serial.print(blueZScore, 2); Serial.print(",");
  Serial.print(lightZScore, 2); Serial.print(",");
  Serial.print(redZScoreWelford, 2); Serial.print(",");
  Serial.print(greenZScoreWelford, 2); Serial.print(",");
  Serial.print(blueZScoreWelford, 2); Serial.print(",");
  Serial.print(lightZScoreWelford, 2); Serial.print(",");
  Serial.print(redCUSUM, 2); Serial.print(",");
  Serial.print(greenCUSUM, 2); Serial.print(",");
  Serial.print(blueCUSUM, 2); Serial.print(",");
  Serial.println(lightCUSUM, 2);
  ten--;
  if (ten == 0){
    Serial.print(orgSum); Serial.print(",");
    Serial.print(wfdSum); Serial.print(",");
    Serial.print(msSum); Serial.print(",");
    Serial.println(usedStackSpace());
    msSum = 0;
    wfdSum = 0;
    orgSum = 0;
    ten = 10;
  }

}
