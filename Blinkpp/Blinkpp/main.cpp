#define F_CPU 16000000UL

#include <avr/io.h>
#include <util/delay.h>

#define setbit(var,bit) (var |= (0x01 << (bit)))
#define clrbit(var,bit) (var &= (~(0x01 << (bit))))
#define set4mhz() CLKPR = 0b10000000; CLKPR = 0b00000001

int main(void)
{
    asm("nop\n");

    set4mhz();

    DDRB |= 0b00100000;
    while (1)
    {
        setbit(PORTB, PINB5);
        _delay_ms(10);
        clrbit(PORTB, PINB5);
        _delay_ms(10);
    }
}
