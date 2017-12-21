#define F_CPU 16000000UL

#include <avr/io.h>
#include <util/delay.h>
#include <math.h>

#include <stdio.h>
#include <stdlib.h>

#include "nokia5110.h"

int main(void)
{
    nokia_lcd_init();
    nokia_lcd_clear();

    ADMUX = (1 << ADLAR) | (1 << REFS0);
    ADCSRA = (1 << ADEN) | (1 << ADSC) | (1 << ADPS2) | (1 << ADPS0);

    while (1)
    {
        while (ADCSRA & (1<<ADSC));
        nokia_lcd_clear();

        const uint8_t samplesNumber = 200;
        uint8_t adcData[samplesNumber];

        for (int i=0; i<samplesNumber;++i)
        {
            ADCSRA |= (1 << ADSC);
            while (ADCSRA & (1<<ADSC));
            uint8_t value = (uint8_t)(((float)ADCH)/(6.0));
            adcData[i] = value;

            _delay_us(162); // -38
        }

        uint8_t pvalue = 0;
        uint8_t max = 0;
        uint8_t min = 48;
        for (int i=0; i<84;++i)
        {
            uint8_t value = adcData[i];

            if (i == 0)
            {
                pvalue = value;
            }
            
            if (max < value)
            {
                max = value;
            }

            if (min > value)
            {
                min = value;
            }

            nokia_lcd_set_pixel(i, 46 - value, 1);

            uint8_t min = value > pvalue ? pvalue : value;
            uint8_t max = value > pvalue ? value : pvalue;

            for (uint8_t j = min; j<= max;++j)
            {
                nokia_lcd_set_pixel(i, 46 - j, 1);
            }

            pvalue = value;
        }

        uint8_t middle = (max-min)/2;
        uint8_t zeroCross[samplesNumber-1];
        uint8_t zeroCrossCount = 0;

        for (int i=1; i<samplesNumber;++i)
        {
            uint8_t prevValue = adcData[i - 1];
            uint8_t value = adcData[i];

            if ((prevValue < middle && value >= middle) ||
            (prevValue >= middle && value < middle))
            {
                zeroCross[zeroCrossCount] = i;
                ++zeroCrossCount;
            }
        }

        if (zeroCrossCount > 1)
        {
            uint8_t frequencies[zeroCrossCount - 1];
            uint8_t frequenciesCount = 0;
            for (int i=1; i<zeroCrossCount;++i)
            {
                float time = (zeroCross[i]-zeroCross[i-1])*0.0002;

                uint8_t freq = 0.5 / time;
                bool has = false;

                for (uint8_t j=0; j < frequenciesCount && !has; ++j)
                {
                    has = frequencies[j] == freq;
                }
                if (!has)
                {
                    frequencies[frequenciesCount] = freq;
                    ++frequenciesCount;
                }
            }

            for (int i=0; i<frequenciesCount;++i)
            {
                nokia_lcd_set_cursor(1, i*7 + i + 1);
                char str[16];

                itoa(frequencies[i], str, 10);

                nokia_lcd_write_string(str, 1);
            }
            
        }

        nokia_lcd_render();
        _delay_ms(2000);
    }
}
