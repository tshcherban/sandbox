#define F_CPU 16000000UL

#include <avr/io.h>
#include <util/delay.h>
#include <math.h>

#include <stdio.h>
#include <stdlib.h>

#include "nokia5110.h"

const uint16_t samplesNumber = 84;
uint8_t samples[samplesNumber];

uint16_t zeroCrossNumber;
uint16_t zeroCrossCurrent;
uint32_t zeroCrossSum;
uint16_t zeroCrossMax;
uint16_t zeroCrossMin;
uint16_t zeroCrossAvg;

uint8_t samplesMax;
uint8_t samplesMin;
uint8_t samplesAvg;

void display(void);
void stats(void);
void getFrequency(void);


int main(void)
{
    nokia_lcd_init();

    ADMUX = (1 << ADLAR) | (1 << REFS0);
    ADCSRA = (1 << ADEN) | (1 << ADSC) | (1 << ADPS2) | (1 << ADPS0);

    while (1)
    {
        while (ADCSRA & (1<<ADSC));

        for (uint16_t i=0; i<samplesNumber; ++i)
        {
            ADCSRA |= (1 << ADSC);
            while (ADCSRA & (1<<ADSC));
            samples[i] = ADCH;

            _delay_us(272); // -28
        }

        nokia_lcd_clear();

        display();
        stats();
        getFrequency();

        nokia_lcd_render();
        _delay_ms(2000);
    }
}

void display(void)
{
    uint8_t pvalue = 0;
    for (uint8_t i = 0; i < 84; ++i)
    {
        uint8_t value = samples[i];
        value = (uint8_t)(((float)value)/(6.0));

        if (i == 0)
        {
            pvalue = value;
        }

        nokia_lcd_set_pixel(i, 46 - value, 1);

        uint8_t minl = value > pvalue ? pvalue : value;
        uint8_t maxl = value > pvalue ? value : pvalue;

        for (uint8_t j = minl; j<= maxl; ++j)
        {
            nokia_lcd_set_pixel(i, 46 - j, 1);
        }

        pvalue = value;
    }
}

void stats(void)
{
    samplesMax = 0;
    samplesMin = 255;
    for (uint16_t i=0; i<samplesNumber; ++i)
    {
        uint8_t value = samples[i];

        if (samplesMax < value)
        {
            samplesMax = value;
        }

        if (samplesMin > value)
        {
            samplesMin = value;
        }
    }

    samplesAvg = (samplesMax-samplesMin)/2;
}

void getFrequency(void)
{
    uint16_t zeroCrossPrev = 0;
    zeroCrossSum = 0;
    zeroCrossNumber = 0;
    for (uint16_t i = 1; i < samplesNumber; ++i)
    {
        uint8_t prevValue = samples[i - 1];
        uint8_t value = samples[i];

        if ((prevValue < samplesAvg && value >= samplesAvg) ||
        (prevValue >= samplesAvg && value < samplesAvg))
        {
            zeroCrossCurrent = i - zeroCrossPrev;

            if (zeroCrossMax < zeroCrossCurrent)
            {
                zeroCrossMax  = zeroCrossCurrent;
            }

            if (zeroCrossMin > zeroCrossCurrent)
            {
                zeroCrossMin = zeroCrossCurrent;
            }
            
            zeroCrossSum += zeroCrossCurrent;
            zeroCrossNumber++;
        }
    }

    if (zeroCrossNumber <= 1)
    {
        return;
    }

    zeroCrossAvg = zeroCrossSum / zeroCrossNumber;

    int frequency = (0.5 / ((float)zeroCrossAvg * 0.0003));

    nokia_lcd_set_cursor(1, 1);
    char str[16];
    itoa((int)frequency, str, 10);
    nokia_lcd_write_string(str, 1);
}