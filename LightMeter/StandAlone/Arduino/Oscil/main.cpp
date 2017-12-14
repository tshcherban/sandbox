#define F_CPU 16000000UL

#include <avr/io.h>
#include <util/delay.h>
#include <math.h>

#include "nokia5110.h"

volatile uint8_t adc[84];

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
        uint8_t pvalue = 0;
        for (int i=0; i<84;++i)
        {
            ADCSRA |= (1 << ADSC);
            while (ADCSRA & (1<<ADSC));
            uint8_t value = (uint8_t)(((float)ADCH)/(6.0));
            
            if (i == 0)
            {
                pvalue = value;
            }
            
            nokia_lcd_set_pixel(i, 46 - value, 1);

            uint8_t min = value > pvalue ? pvalue : value;
            uint8_t max = value > pvalue ? value : pvalue;

            for (uint8_t j = min; j<= max;++j)
            {
                nokia_lcd_set_pixel(i, 46 - j, 1);
            }

            pvalue=value;
            
            _delay_us(300);
        }

        nokia_lcd_render();
        _delay_ms(2000);
    }
}
