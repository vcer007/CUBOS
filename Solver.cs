﻿using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CUBOS
{
    class SolverSinglePhase
    {
        //this variable is used when determining if a weel is capable of producing fluid under the specified flow rate
        private static bool skip;

        //#############################################################################################
        //Method Name: incompressible
        //Objectives: Solver for the single-phase incompressible fluid
        //The solution is time-independent
        //Inputs: an array of variables of type "GridBlock"
        //Outputs: N/A
        public static void incompressible(GridBlock[] grid)
        {
            int grid_length = grid.Length;
            double[][] matrix_P = new double[grid_length][];
            double[] matrix_C = new double[grid_length];

            //Parallel.For(0, grid_length, (i) => {

            //});


            GridBlock block;
            GridBlock next_block;
            var phase = Transmissibility.Phase.Water;

            double temp = 0;
            double constants = 0;
            double T = 0;
            double[] new_P;

            for (int i = 0; i < grid_length; i++)
            {
                matrix_P[i] = new double[grid_length];

                block = grid[i];
                temp = 0;
                constants = 0;
                T = 0;

                //#########################################################################################
                //Calculate transmissibilities
                if (block.east_counter != -1)
                {
                    next_block = grid[block.east_counter];
                    T = Transmissibility.getTransmissibility(block, next_block, block.GF_x, phase);
                    matrix_P[i][next_block.counter] = T;
                    temp -= T;
                }

                if (block.west_counter != -1)
                {
                    next_block = grid[block.west_counter];
                    T = Transmissibility.getTransmissibility(block, next_block, block.GF_x, phase);
                    matrix_P[i][next_block.counter] = T;
                    temp -= T;
                }

                if (block.north_counter != -1)
                {
                    next_block = grid[block.north_counter];
                    T = Transmissibility.getTransmissibility(block, next_block, block.GF_y, phase);
                    matrix_P[i][next_block.counter] = T;
                    temp -= T;
                }

                if (block.south_counter != -1)
                {
                    next_block = grid[block.south_counter];
                    T = Transmissibility.getTransmissibility(block, next_block, block.GF_y, phase);
                    matrix_P[i][next_block.counter] = T;
                    temp -= T;
                }

                //#########################################################################################
                //check boundary conditions
                //Specified boundary pressure
                constants -= block.boundary_p;
                temp -= block.boundary_T;

                //Specified boundary rate
                constants += block.boundary_q;

                //Specified boundary pressure gradient
                constants += block.boundary_gradient;

                //#########################################################################################
                //check well rates
                //Production
                if (block.type == GridBlock.Type.Well_Production)
                {
                    //Check for specified BHP condition
                    if (block.well_type == GridBlock.WellType.Specified_BHP)
                    {
                        constants -= block.well_transmissibility * block.BHP;
                        temp -= block.well_transmissibility;
                    }
                    else
                    {
                        constants += block.specified_flow_rate;
                    }
                }
                //Injection
                else
                {
                    //Check for specified BHP condition
                    if (block.well_type == GridBlock.WellType.Specified_BHP)
                    {
                        constants -= block.well_transmissibility * block.BHP;
                        temp -= block.well_transmissibility;
                    }
                    else
                    {
                        constants += block.specified_flow_rate;
                    }
                }

                //#########################################################################################
                matrix_P[i][block.counter] = temp;
                matrix_C[i] = constants;
            }

            new_P = solveForP(matrix_P, matrix_C);


            Console.WriteLine(new_P[0] + ", " + new_P.Last());
        }



        //#############################################################################################
        //Method Name: slightly_compressible
        //Objectives: Solver for the single-phase slightly compressible fluid
        //The solution is time-dependent
        //Inputs: an array of variables of type "GridBlock", the value of the time step between subsequent runs and the value of the total time duration
        //Outputs: N/A
        public static void slightly_compressible(GridBlock[] grid, double delta_t, double time_max)
        {
            int grid_length = grid.Length;
            double[][] matrix_P = new double[grid_length][];
            double[] matrix_C = new double[grid_length];

            const double a = 5.614583;
            double accumulation_term;

            GridBlock block;
            GridBlock next_block;
            var phase = Transmissibility.Phase.Water;

            double temp = 0;
            double constants = 0;
            double T = 0;
            double[] new_P;

            for (double current_time = 0; current_time < time_max; current_time += delta_t)
            {
                skip = false;

                #region grid blocks loop
                for (int i = 0; i < grid_length; i++)
                {
                    matrix_P[i] = new double[grid_length];

                    block = grid[i];
                    temp = 0;
                    constants = 0;
                    T = 0;
                    accumulation_term = 0;

                    //#########################################################################################
                    //Calculate transmissibilities
                    if (block.east_counter != -1)
                    {
                        next_block = grid[block.east_counter];
                        T = Transmissibility.getTransmissibility(block, next_block, block.GF_x, phase);
                        matrix_P[i][next_block.counter] = T;
                        temp -= T;
                    }

                    if (block.west_counter != -1)
                    {
                        next_block = grid[block.west_counter];
                        T = Transmissibility.getTransmissibility(block, next_block, next_block.GF_x, phase);
                        matrix_P[i][next_block.counter] = T;
                        temp -= T;
                    }

                    if (block.north_counter != -1)
                    {
                        next_block = grid[block.north_counter];
                        T = Transmissibility.getTransmissibility(block, next_block, block.GF_y, phase);
                        matrix_P[i][next_block.counter] = T;
                        temp -= T;
                    }

                    if (block.south_counter != -1)
                    {
                        next_block = grid[block.south_counter];
                        T = Transmissibility.getTransmissibility(block, next_block, next_block.GF_y, phase);
                        matrix_P[i][next_block.counter] = T;
                        temp -= T;
                    }

                    //#########################################################################################
                    //check boundary conditions
                    //Specified boundary pressure
                    constants -= block.boundary_p;
                    temp -= block.boundary_T;

                    //Specified boundary rate
                    constants += block.boundary_q;

                    //Specified boundary pressure gradient
                    constants += block.boundary_gradient;

                    //#########################################################################################
                    //check well rates
                    //Production
                    if (block.type == GridBlock.Type.Well_Production)
                    {
                        //Check for specified BHP condition
                        if (block.well_type == GridBlock.WellType.Specified_BHP)
                        {
                            constants -= block.well_transmissibility * block.BHP;
                            temp -= block.well_transmissibility;
                        }
                        else
                        {
                            constants += block.specified_flow_rate;
                        }
                    }
                    //Injection
                    else
                    {
                        //Check for specified BHP condition
                        if (block.well_type == GridBlock.WellType.Specified_BHP)
                        {
                            constants -= block.well_transmissibility * block.BHP;
                            temp -= block.well_transmissibility;
                        }
                        else
                        {
                            constants += block.specified_flow_rate;
                        }
                    }

                    //#########################################################################################
                    //Accumulation terms
                    accumulation_term = block.bulk_volume * block.porosity * (block.Cf + block.Cw) / (a * block.Bw * delta_t);
                    constants -= accumulation_term * block.pressure;
                    temp -= accumulation_term;

                    //#########################################################################################
                    matrix_P[i][block.counter] = temp;
                    matrix_C[i] = constants;
                }
                #endregion

                new_P = solveForP(matrix_P, matrix_C);

                #region This code handle the transformation of a specified flow_rate well into a specified BHP well
                for (int i = 0; i < grid.Length; i++)
                {
                    block = grid[i];
                    if (block.type == GridBlock.Type.Well_Production)
                    {
                        if (block.well_type == GridBlock.WellType.Specified_Flow_Rate)
                        {
                            double BHP = new_P[i] - block.specified_flow_rate / block.well_transmissibility;
                            if (BHP > block.BHP_minimum)
                            {
                                block.BHP = BHP;
                            }
                            //If the well can no longer produce the fluid at the specified flow rate, convert the well to a specified BHP type
                            //
                            else
                            {
                                block.well_type = GridBlock.WellType.Specified_BHP;
                                block.BHP = block.BHP_minimum;
                                //Re do this iteration as the BHP of the well went below the minimum
                                current_time -= delta_t;
                                skip = true;
                                break;
                            }
                        }
                    }
                }
                #endregion


                if (!skip)
                {
                    update_properties_chord_slope(new_P, grid);
                    Console.WriteLine(new_P[0] + ", " + new_P[1] + ", " + new_P[2] + ", " + new_P[3]);
                    Console.ReadKey();
                }
            }

        }

        //Method Name: update_properties_chord_slope
        //Objectives: uses the fluid and rock compressibilities to calculate the new properties for the slightly compressible solver
        //Inputs: values of the new pressures and the array representing the grid blocks
        //Outputs: this method internally updates the values of the fluid PVT and rock properties of each grid block
        private static void update_properties_chord_slope(double[] new_P, GridBlock[] grid)
        {
            GridBlock block;
            for (int i = 0; i < grid.Length; i++)
            {
                block = grid[i];
                block.pressure = new_P[i];
                block.Bw = PVT.chord_slope_FVF(block.Cw, block.Bw, new_P[i], block.pressure);
                block.porosity = Vp_Calculator.chord_slope_Vp(block.Cf, block.porosity, new_P[i], block.pressure);
                if (block.well_transmissibility != 0)
                {
                    block.well_transmissibility = Well.getTransmissibility(block, block.well_geometric_factor, Well.Phase.Water);
                }
            }
        }



        //#############################################################################################
        //Method Name: compressible
        //Objectives: Solver for the single-phase slightly compressible fluid
        //The solution is time-dependent
        //Inputs: an array of variables of type "GridBlock", the value of the time step between subsequent runs and the value of the total time duration
        //Outputs: N/A
        public static void compressible(GridBlock[] grid, double delta_t, double time_max, double convergence_pressure, PVT pvt)
        {
            int grid_length = grid.Length;

            double[][] matrix_P = new double[grid_length][];
            double[] matrix_C = new double[grid_length];
            double[] new_P = new double[grid_length];
            double[] old_iteration_P = new double[grid_length];
            double new_pressure = 0;

            const double a = 5.614583;
            double accumulation_term;

            //The non-linear pressure solution convergence maximum number of iterations
            const int iterations_max = 10;
            double new_phi;
            double new_FVF;
            double delta_phi_by_FVF;
            double delta_p;

            GridBlock block;
            GridBlock next_block;
            var phase = Transmissibility.Phase.Water;

            double temp = 0;
            double constants = 0;
            double T = 0;

            for (double current_time = 0; current_time < time_max; current_time += delta_t)
            {
                //Loop non-linear iterations
                //Break out of the loop if the pressure solution converges to the specified criterion or after a certain number of iterations whichever is sooner
                for (int iteration = 0; iteration < iterations_max; iteration++)
                {

                    #region grid blocks loop
                    for (int i = 0; i < grid_length; i++)
                    {
                        matrix_P[i] = new double[grid_length];

                        block = grid[i];
                        temp = 0;
                        constants = 0;
                        T = 0;
                        accumulation_term = 0;

                        //#########################################################################################
                        //Calculate transmissibilities
                        if (block.east_counter != -1)
                        {
                            next_block = grid[block.east_counter];
                            T = Transmissibility.getTransmissibility(block, next_block, block.GF_x, phase);
                            matrix_P[i][next_block.counter] = T;
                            temp -= T;
                        }

                        if (block.west_counter != -1)
                        {
                            next_block = grid[block.west_counter];
                            T = Transmissibility.getTransmissibility(block, next_block, next_block.GF_x, phase);
                            matrix_P[i][next_block.counter] = T;
                            temp -= T;
                        }

                        if (block.north_counter != -1)
                        {
                            next_block = grid[block.north_counter];
                            T = Transmissibility.getTransmissibility(block, next_block, block.GF_y, phase);
                            matrix_P[i][next_block.counter] = T;
                            temp -= T;
                        }

                        if (block.south_counter != -1)
                        {
                            next_block = grid[block.south_counter];
                            T = Transmissibility.getTransmissibility(block, next_block, next_block.GF_y, phase);
                            matrix_P[i][next_block.counter] = T;
                            temp -= T;
                        }

                        //#########################################################################################
                        //check boundary conditions
                        //Specified boundary pressure
                        constants -= block.boundary_p;
                        temp -= block.boundary_T;

                        //Specified boundary rate
                        constants += block.boundary_q;

                        //Specified boundary pressure gradient
                        constants += block.boundary_gradient;

                        //#########################################################################################
                        //check well rates
                        //Production
                        if (block.type == GridBlock.Type.Well_Production)
                        {
                            //Check for specified BHP condition
                            if (block.well_type == GridBlock.WellType.Specified_BHP)
                            {
                                constants -= block.well_transmissibility * block.BHP;
                                temp -= block.well_transmissibility;
                            }
                            else
                            {
                                constants += block.specified_flow_rate;
                            }
                        }
                        //Injection
                        else
                        {
                            //Check for specified BHP condition
                            if (block.well_type == GridBlock.WellType.Specified_BHP)
                            {
                                constants -= block.well_transmissibility * block.BHP;
                                temp -= block.well_transmissibility;
                            }
                            else
                            {
                                constants += block.specified_flow_rate;
                            }
                        }

                        //#########################################################################################
                        //Accumulation terms
                        if (iteration == 0)
                        {
                            new_pressure = block.pressure - 1;
                        }
                        else
                        {
                            new_pressure = new_P[i];
                        }

                        new_phi = Vp_Calculator.chord_slope_Vp(block.Cf, block.porosity, new_pressure, block.pressure);
                        new_FVF = pvt.getWaterFVF(new_pressure);
                        delta_phi_by_FVF = (new_phi / new_FVF) - (block.porosity / block.Bw);
                        delta_p = new_pressure - block.pressure;

                        accumulation_term = block.bulk_volume / (a * delta_t) * delta_phi_by_FVF / delta_p;
                        constants -= accumulation_term * block.pressure;
                        temp -= accumulation_term;

                        //#########################################################################################
                        matrix_P[i][i] = temp;
                        matrix_C[i] = constants;
                    }
                    #endregion

                    new_P = solveForP(matrix_P, matrix_C);

                    //Check for convergence
                    if (iteration != 0)
                    {
                        //check
                        if (check_pressure_convergence(new_P, old_iteration_P) <= convergence_pressure)
                        {
                            break;
                        }
                    }

                    old_iteration_P = new_P.Clone() as double[];
                }


                #region This code handle the transformation of a specified flow_rate well into a specified BHP well
                for (int i = 0; i < grid.Length; i++)
                {
                    block = grid[i];
                    if (block.type == GridBlock.Type.Well_Production)
                    {
                        if (block.well_type == GridBlock.WellType.Specified_Flow_Rate)
                        {
                            double BHP = new_P[i] - block.specified_flow_rate / block.well_transmissibility;
                            if (BHP > block.BHP_minimum)
                            {
                                block.BHP = BHP;
                            }
                            //If the well can no longer produce the fluid at the specified flow rate, convert the well to a specified BHP type
                            //
                            else
                            {
                                block.well_type = GridBlock.WellType.Specified_BHP;
                                block.BHP = block.BHP_minimum;
                                //Re do this iteration as the BHP of the well went below the minimum
                                current_time -= delta_t;
                                skip = true;
                                break;
                            }
                        }
                    }
                }
                #endregion


                if (!skip)
                {
                    update_properties_compressible(new_P, grid, pvt);
                    Console.WriteLine(new_P[0] + ", " + new_P[1] + ", " + new_P[2] + ", " + new_P[3]);
                    Console.ReadKey();
                }
            }

        }

        //Method Name: update_properties_compressible
        //Objectives: uses the PVT properties table and rock compressibilities to calculate the new properties for the compressible solver
        //Inputs: values of the new pressures, the array representing the grid blocks and a variable of type "PVT" that makes table look-up and interpolations for new PVT values
        //Outputs: this method internally updates the values of the fluid PVT and rock properties of each grid block
        private static void update_properties_compressible(double[] new_P, GridBlock[] grid, PVT pvt)
        {
            GridBlock block;
            double new_pressure;
            for (int i = 0; i < grid.Length; i++)
            {
                block = grid[i];
                new_pressure = new_P[i];

                block.Bw = pvt.getWaterFVF(new_pressure);
                block.water_viscosity = pvt.getWaterViscosity(new_pressure);

                block.porosity = Vp_Calculator.chord_slope_Vp(block.Cf, block.porosity, new_pressure, block.pressure);

                block.pressure = new_pressure;

                if (block.well_transmissibility != 0)
                {
                    block.well_transmissibility = Well.getTransmissibility(block, block.well_geometric_factor, Well.Phase.Water);
                }
            }
        }

        //Method Name: check_pressure_convergence
        //Objectives: used to check if the pressure solution converges through the non-linear iterations
        //Inputs: two arrays with the values of the old and new grid blocks pressures
        //Outputs: the maximum change in pressure that occurs in any of the grid blocks between two successive non-linear iterations
        //The output is compared against a specified criterion of convergence
        private static double check_pressure_convergence(double[] new_P, double[] old_P)
        {
            int length = new_P.Length;
            double[] temp = new double[length];
            for (int i = 0; i < length; i++)
            {
                temp[i] = Math.Abs(new_P[i] - old_P[i]) / old_P[i];
            }

            return temp.Max();
        }



        //#############################################################################################
        //
        private static double[] solveForP(double[][] matrix_P, double[] matrix_C)
        {
            Matrix<double> A = MathNet.Numerics.LinearAlgebra.Double.SparseMatrix.OfRowArrays(matrix_P);
            Vector<double> B = MathNet.Numerics.LinearAlgebra.Double.DenseVector.OfArray(matrix_C);

            Vector<double> temp = A.Solve(B);

            return temp.ToArray();
        }
    }
}