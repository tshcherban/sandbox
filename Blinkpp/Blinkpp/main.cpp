#define F_CPU 16000000UL
//#define F_CPU 62500UL

#include <avr/io.h>
#include <util/delay.h>

#define setbit(var,bit) (var |= (0x01 << (bit)))
#define clrbit(var,bit) (var &= (~(0x01 << (bit))))
#define set4mhz() CLKPR = 0b10000000; CLKPR = 0b00000010
#define set62_5khz() CLKPR = 0b10000000; CLKPR = 0b00001000

int main(void)
{
    asm("nop\n");

    set62_5khz();

    DDRB |= 0b00100000;
    while (1)
    {
        setbit(PORTB, PINB5);
        _delay_us(500);
        clrbit(PORTB, PINB5);
        _delay_us(500);
    }
}
