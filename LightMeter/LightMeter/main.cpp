#include <avr/io.h>
#include <avr/interrupt.h>

#define UART_PIN PIND5
#define ADC_PIN PIND4

// timer 0 clock sources
#define T0CL_0 0
#define T0CL_1 1
#define T0CL_8 2
#define T0CL_64 3
#define T0CL_256 4
#define T0CL_1024 5
#define T0CL_EXT_FALL 6
#define T0CL_EXT_RISE 7

inline void setup();

bool bt;

ISR(TIMER0_COMPA_vect)
{
    if (bt)
    {
        PORTD |= (1 << UART_PIN);
        bt = false;
    }
    else
    {
        PORTD &= ~(1 << UART_PIN);
        bt = true;
    }
}

int main(void)
{
    setup();

    while (1)
    {
        TCCR0A = (1 << COM0A0) | (1 << WGM01);
        TCCR0B = (T0CL_8 << CS00);
        TIMSK0 = (1 << OCIE0A);
        OCR0A = 119;
        TCNT0 = 0;
        sei();

        while (1)
        {
            asm("nop;");
            asm("nop;");
            asm("nop;");
            asm("nop;");
        }

        while (ADCSRA & (1 << ADSC));
        while (true) {
            
            while (ADCSRA & (1 << ADSC));
            PORTD &= ~(1 << ADC_PIN);

            PORTD |= (1 << UART_PIN);
            UCSR0A |= 1 << TXC0;
            UDR0 = ADCH;
            ADCSRA |= (1 << ADSC);
            PORTD |= (1 << ADC_PIN);
            while ((UCSR0A & (1 << TXC0)) == 0);
            PORTD &= ~(1 << UART_PIN);
        }
    }
}

void setup()
{
    DDRD |= (1 << ADC_PIN) | (1 << UART_PIN) | (1 << PIND6);

    UCSR0B = (1 << RXEN0) | (1 << TXEN0);
    UCSR0C = (1 << USBS0) | (3 << UCSZ00);
    UCSR0A = (1 << U2X0);
    UBRR0L = 0;
    UBRR0H = 0;

    ADMUX = (1 << ADLAR) | (1 << REFS1) | (1 << REFS0);
    ADCSRA = (1 << ADEN) | (1 << ADSC) | (1 << ADPS2) | (1 << ADPS1);
}