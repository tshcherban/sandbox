namespace LightMeter
{
    public class Kalman
    {
        // kalman variables
        private double varVolt = 1e-06;

        private double varProcess = 0.8e-8;
        private double Pc = 0.0;
        private double G = 0.0;
        private double P = 1.0;
        private double Xp = 0.0;
        private double Zp = 0.0;
        private double Xe = 0.0;

        public double GetValue(double value)
        {
            // kalman process
            Pc = P + varProcess;
            G = Pc / (Pc + varVolt); // kalman gain
            P = (1 - G) * Pc;
            Xp = Xe;
            Zp = Xp;
            Xe = G * (value - Zp) + Xp; // the kalman estimate of the sensor voltage

            return Xe;
        }
    }
}