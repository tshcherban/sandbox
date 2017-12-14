#define F_CPU 16000000UL

#include <avr/io.h>
#include <util/delay.h>
#include <avr/interrupt.h>

// pins for debugging purposes
#define UART_PIN PIND5
#define ADC_PIN PIND4

// timer 0 clock sources and prescalers
#define T0CL_0 0
#define T0CL_1 1
#define T0CL_8 2
#define T0CL_64 3
#define T0CL_256 4
#define T0CL_1024 5
#define T0CL_EXT_FALL 6
#define T0CL_EXT_RISE 7

#define SET_BIT(BYTE, BIT) BYTE |= (1 << BIT)
#define CLR_BIT(BYTE, BIT) BYTE &= ~(1 << BIT)
#define TST_BIT(BYTE, BIT) (BYTE & (1 << BIT))

// timer 0 interrupt frequency, calculated for clock CPU 16 MHz, prescaler 64 and ctc mode
#define FREQ_62_5 3
#define FREQ_50_0 4
#define FREQ_31_25 7
#define FREQ_25_0 9
#define FREQ_15_625 15
#define FREQ_12_5 19
#define FREQ_10_0 24
#define FREQ_6_25 39
#define FREQ_5_0 49
#define FREQ_2_0 124
#define FREQ_1_0 249

// flags
#define ALLOW_TRANSMIT 0
#define IN_COMMAND 7

volatile uint8_t command[8] = {0, 0, 0, 0, 0, 0, 0, 0};
volatile uint8_t counter = 0;
volatile uint16_t samplesNumber;
volatile uint8_t flags = 0;

inline void setup();

ISR(USART_RX_vect)
{
    uint8_t data = UDR0;
    if (counter < 8)
    {
        command[counter] = data;
        ++counter;
    }
}

ISR(ADC_vect)
{
    uint8_t result = ADCH;
    if (TST_BIT(flags, ALLOW_TRANSMIT))
    {
        UDR0 = result;
    }
}

ISR(TIMER0_COMPA_vect)
{
    if (samplesNumber == 0)
    {
        CLR_BIT(TIMSK0 , OCIE0A);
        CLR_BIT(flags, ALLOW_TRANSMIT);
        counter = 0;
    }
    else
    {
        SET_BIT(ADCSRA, ADSC);
        --samplesNumber;
    }
}

int main(void)
{
    setup();

    while (1)
    {
        if (counter >= 8)
        {
            if (command[0] == 49)
            {
                samplesNumber = ((command[1] << 8) | command[2]) + 1;
                volatile uint16_t samplesNumber1 = 30000;
                while (samplesNumber1 > 0)
                {
                    SET_BIT(ADCSRA, ADSC);
                    while (TST_BIT(ADCSRA, ADSC));
                    --samplesNumber1;
                }

                SET_BIT(flags, ALLOW_TRANSMIT);
                SET_BIT(TIMSK0 , OCIE0A);
                command[0] = 0;
            }
        }
    }
}

void setup()
{
    DDRD |= (1 << ADC_PIN) | (1 << UART_PIN) | (1 << PIND6);

    UCSR0B = (1 << RXEN0) | (1 << TXEN0) | (1 << RXCIE0);
    UCSR0C = (1 << USBS0) | (3 << UCSZ00);
    UCSR0A = (1 << U2X0);
    UBRR0 = 1;

    ADMUX = (1 << ADLAR) | (1 << REFS1) | (1 << REFS0);
    ADCSRA = (1 << ADEN) | (1 << ADSC) | (1 << ADPS2) | (1 << ADPS0) | (1 << ADIE);
    DIDR0 = 0xFF;

    TCCR0A = /*(1 << COM0A0) |*/ (1 << WGM01);
    TCCR0B = (T0CL_64 << CS00);
    //TIMSK0 = (1 << OCIE0A);
    OCR0A = FREQ_31_25;
    TCNT0 = 0;

    sei();
}