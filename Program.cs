using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Gurobi;
using Newtonsoft.Json;
using System.ComponentModel;

namespace RelaxAndFix_cs
{
    class Program
    {
        static void Main(string[] args)
        {
            string[] filePaths = Directory.GetFiles(@"./", "*.json");

            foreach (string arquivo in filePaths)
            {
                //Deserializar o arquivo Json para o c#
                var json = File.ReadAllText(arquivo);
                Parametros instancia = JsonConvert.DeserializeObject<Parametros>(json);

                //Modelo
                GRBEnv ambiente = new GRBEnv();
                GRBModel modelo = new GRBModel(ambiente);
                modelo.ModelName = "Crew Scheduling Problem";

                //número grande
                int M = 1000;

                //Variáveis

                //fração da tarefa i que o membro de equipe n completa na data j
                GRBVar[,,] x = new GRBVar[instancia.crew, instancia.task, instancia.date];
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int i = 0; i < instancia.task; i++)
                    {
                        for (int j = 0; j < instancia.date; j++)
                        {
                            x[n, i, j] = modelo.AddVar(0, 1, 0, GRB.CONTINUOUS, "x_" + n + "_" + i + "_" + j);
                        }
                    }
                }
                //fração da tarefa i que é completada na data j
                GRBVar[,] x3 = new GRBVar[instancia.task, instancia.date];
                for (int i = 0; i < instancia.task; i++)
                {
                    for (int j = 0; j < instancia.date; j++)
                    {
                        x3[i, j] = modelo.AddVar(0, 1, 0, GRB.CONTINUOUS, "x3_" + i + "_" + j);
                    }
                }
                //1 se alguma tarefa i é completada na data j
                GRBVar[,] x2 = new GRBVar[instancia.task, instancia.date];
                for (int i = 0; i < instancia.task; i++)
                {
                    for (int j = 0; j < instancia.date; j++)
                    {
                        x2[i, j] = modelo.AddVar(0, 1, 0, GRB.BINARY, "x2_" + i + "_" + j);
                    }
                }
                //1 se a tarefa i é concluída dentro do horizonte de planejamento
                GRBVar[] x4 = new GRBVar[instancia.task];
                for (int i = 0; i < instancia.task; i++)
                {
                    x4[i] = modelo.AddVar(0, 1, -instancia.c[i] * 2, GRB.BINARY, "x4_" + i);
                }
                //variável fantasma
                GRBVar[] vf = new GRBVar[instancia.task];
                for (int i = 0; i < instancia.task; i++)
                {
                    vf[i] = modelo.AddVar(1, 1, instancia.c[i] * 2, GRB.BINARY, "vf_" + i);
                }
                //1 se o membro de equipe n está trabalhando na tarefa i na data j mas não na data j+1
                GRBVar[,,] x5 = new GRBVar[instancia.crew, instancia.task, instancia.date];
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int i = 0; i < instancia.task; i++)
                    {
                        for (int j = 0; j < instancia.date; j++)
                        {
                            x5[n, i, j] = modelo.AddVar(0, 1, 0.1, GRB.BINARY, "x5_" + n + "_" + i + "_" + j);
                        }
                    }
                }
                //1 se parte da tarefa i é completada na data j mas não na data j+1
                GRBVar[,] x6 = new GRBVar[instancia.task, instancia.date];
                for (int i = 0; i < instancia.task; i++)
                {
                    for (int j = 0; j < instancia.date; j++)
                    {
                        x6[i, j] = modelo.AddVar(0, 1, 0.9, GRB.BINARY, "x6_" + i + "_" + j);
                    }
                }
                //1 se o membro da equipe n vai trabalhar na data j
                GRBVar[,] y = new GRBVar[instancia.crew, instancia.date];
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int j = 0; j < instancia.date; j++)
                    {
                        y[n, j] = modelo.AddVar(0, 1, instancia.hours_per_shift, GRB.BINARY, "y_" + n + "_" + j);
                    }
                }
                //1 se membro da equipe n trabalha na tarefa i na data j
                GRBVar[,,] z = new GRBVar[instancia.crew, instancia.task, instancia.date];
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int i = 0; i < instancia.task; i++)
                    {
                        for (int j = 0; j < instancia.date; j++)
                        {
                            z[n, i, j] = modelo.AddVar(0, 1, 0.5, GRB.BINARY, "z_" + n + "_" + i + "_" + j);
                        }
                    }
                }
                //1 se o membro de equipe n trabalha na tarefa i
                GRBVar[,] z1 = new GRBVar[instancia.crew, instancia.task];
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int i = 0; i < instancia.task; i++)
                    {
                        z1[n, i] = modelo.AddVar(0, 1, 0.1, GRB.BINARY, "z1_" + n + "_" + i);
                    }
                }
                //1 se o membro de equipe n trabalha no local técnico p na data j
                GRBVar[,,] w = new GRBVar[instancia.crew, instancia.technical_place, instancia.date];
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int p = 0; p < instancia.technical_place; p++)
                    {
                        for (int j = 0; j < instancia.date; j++)
                        {
                            w[n, p, j] = modelo.AddVar(0, 1, 0, GRB.BINARY, "w_" + n + "_" + p + "_" + j);
                        }
                    }
                }
                //1 se o membro de equipe n precisa de transporte entre o local técnico o e o local q na instancia.date j
                GRBVar[,,,] v = new GRBVar[instancia.crew, instancia.technical_place, instancia.technical_place, instancia.date];
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int p = 0; p < instancia.technical_place; p++)
                    {
                        for (int q = 0; q < instancia.technical_place; q++)
                        {
                            for (int j = 0; j < instancia.date; j++)
                            {
                                v[n, p, q, j] = modelo.AddVar(0, 1, 0, GRB.BINARY, "v_" + n + "_" + p + "_" + q + "_" + j);
                            }
                        }
                    }
                }
                //se a equipe n precisa de transporte para o local técnico p de outro local técnico na data j
                GRBVar[,,] w1 = new GRBVar[instancia.crew, instancia.technical_place, instancia.date];
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int p = 0; p < instancia.technical_place; p++)
                    {
                        for (int j = 0; j < instancia.date; j++)
                        {
                            w1[n, p, j] = modelo.AddVar(0, 1, 0, GRB.BINARY, "w1_" + n + "_" + p + "_" + j);
                        }
                    }
                }
                //se a equipe n precisa de transporte do local técnico p para outro local técnico
                GRBVar[,,] w2 = new GRBVar[instancia.crew, instancia.technical_place, instancia.date];
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int p = 0; p < instancia.technical_place; p++)
                    {
                        for (int j = 0; j < instancia.date; j++)
                        {
                            w2[n, p, j] = modelo.AddVar(0, 1, 0, GRB.BINARY, "w2_" + n + "_" + p + "_" + j);
                        }
                    }
                }

                //Função objetivo
                modelo.ModelSense = GRB.MINIMIZE;

                //Restrições
                GRBLinExpr exp = 0.0;
                GRBLinExpr exp2 = 0.0;
                GRBLinExpr exp3 = 0.0;

                //Restrições com relação a tarefa

                //restrição 2 
                //a tarefa deve ser concluída dentro do horizonte de planejamento
                for (int i = 0; i < instancia.task; i++)
                {
                    exp.Clear();
                    for (int n = 0; n < instancia.crew; n++)
                    {
                        for (int j = 0; j < instancia.date; j++)
                        {
                            exp.AddTerm(1, x[n, i, j]);
                        }
                    }
                    modelo.AddConstr(exp == x4[i], "R2_" + i);
                }
                //restrição 3
                //o número total de horas por turno não deve ser excedido
                for (int j = 0; j < instancia.date; j++)
                {
                    for (int n = 0; n < instancia.crew; n++)
                    {
                        exp.Clear();
                        exp2.Clear();
                        exp3.Clear();
                        for (int i = 0; i < instancia.task; i++)
                        {
                            exp.AddTerm(instancia.c[i], x[n, i, j]);
                        }
                        for (int p = 0; p < instancia.technical_place; p++)
                        {
                            exp2.AddTerm(2 * instancia.tm[p], w[n, p, j]);
                            exp2.AddTerm(-instancia.tm[p], w1[n, p, j]);
                            exp2.AddTerm(-instancia.tm[p], w2[n, p, j]);
                        }
                        for (int p = 0; p < instancia.technical_place; p++)
                        {
                            for (int q = 0; q < instancia.technical_place; q++)
                            {
                                exp3.AddTerm(instancia.tr[p, q], v[n, p, q, j]);
                            }
                        }
                        modelo.AddConstr(exp + exp2 + exp3 <= instancia.hours_per_shift, "R3_" + j + "_" + n);
                    }
                }
                //restrição 4
                //a soma das frações das tarefas locadas  não pode exceder o total para completar a tarefa
                for (int i = 0; i < instancia.task; i++)
                {
                    for (int j = 0; j < instancia.date; j++)
                    {
                        exp.Clear();
                        for (int n = 0; n < instancia.crew; n++)
                        {
                            exp.AddTerm(1, x[n, i, j]);
                        }
                        modelo.AddConstr(x2[i, j] >= exp, "R4_" + i + "_" + j);
                    }
                }
                //restrição 5
                //soma de das frações dos membros e equipe num dado dia deve ser igual a x3
                for (int i = 0; i < instancia.task; i++)
                {
                    for (int j = 0; j < instancia.date; j++)
                    {
                        exp.Clear();
                        for (int n = 0; n < instancia.crew; n++)
                        {
                            exp.AddTerm(1, x[n, i, j]);
                        }
                        modelo.AddConstr(x3[i, j] == exp, "R5_" + i + "_" + j);
                    }
                }
                //restrição 6
                //a tarefa i deve ser completada dentro do horizonte de planejamento se gi=1
                for (int i = 0; i < instancia.task; i++)
                {
                    modelo.AddConstr(x4[i] >= instancia.g[i], "R6_" + i);
                }
                //restrição 7
                //fração da tarefa que é completada num dado dia não deve exceder X4
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int i = 0; i < instancia.task; i++)
                    {
                        for (int j = 0; j < instancia.date; j++)
                        {
                            modelo.AddConstr(x4[i] >= x[n, i, j], "R7_" + n + "_" + i + "_" + j);
                        }
                    }
                }
                //restrição 8
                //um membro de equipe não pode ser locado a uma tarefa em um dia em que ele não trabalha
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int i = 0; i < instancia.task; i++)
                    {
                        for (int j = 0; j < instancia.date; j++)
                        {
                            modelo.AddConstr(y[n, j] >= z[n, i, j], "R8_" + n + "_" + i + "_" + j);
                        }
                    }
                }
                //restrição 9
                //se o membro de equipe é locado para uma tarefa então z=1
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int i = 0; i < instancia.task; i++)
                    {
                        for (int j = 0; j < instancia.date; j++)
                        {
                            modelo.AddConstr(z[n, i, j] >= x[n, i, j], "R9_" + n + "_" + i + "_" + j);
                        }
                    }
                }
                //restrição 10
                //a variável z não pode ser 1 se a equipe n não trabalha num dado dia
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int i = 0; i < instancia.task; i++)
                    {
                        for (int j = 0; j < instancia.date; j++)
                        {
                            modelo.AddConstr(z[n, i, j] <= M * x[n, i, j], "R10_" + n + "_" + i + "_" + j);
                        }
                    }
                }
                //restrição 11
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int i = 0; i < instancia.task; i++)
                    {
                        for (int j = 0; j < instancia.date; j++)
                        {
                            modelo.AddConstr(z1[n, i] >= z[n, i, j], "R11_" + n + "_" + i + "_" + j);
                        }
                    }
                }

                //Restrições de gerenciemanto

                //restrição 12
                //preferencalmente uma tarefa deve concluida pela mesma pessoa que começou trabalhando nela
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int i = 0; i < instancia.task; i++)
                    {
                        for (int j = 0; j < instancia.date - 1; j++)
                        {
                            modelo.AddConstr(x5[n, i, j] >= z[n, i, j] - z[n, i, j + 1], "R12_" + n + "_" + i + "_" + j);
                        }
                    }
                }
                //restrição 13
                //uma penalidade será dada ao planejamento se a tarefa i é completada em dias não consecutivos
                for (int i = 0; i < instancia.task; i++)
                {
                    for (int j = 0; j < instancia.date - 1; j++)
                    {
                        modelo.AddConstr(x6[i, j] >= x2[i, j] - x2[i, j + 1], "R13_" + i + "_" + j);
                    }
                }
                //restrição 14
                //o número mínimo de membros de equipe que podem trabalhar simultaneamente em uma tarefa 
                for (int i = 0; i < instancia.task; i++)
                {
                    for (int j = 0; j < instancia.date; j++)
                    {
                        exp.Clear();
                        for (int n = 0; n < instancia.crew; n++)
                        {
                            exp.AddTerm(1, z[n, i, j]);
                        }
                        modelo.AddConstr(exp >= instancia.d1[i] * x2[i, j], "R14_" + i + "_" + j);
                    }
                }
                //restrição 15
                //o número máximo de membros de equipe que podem trablhar simultaneamente em uma tarefa 
                for (int i = 0; i < instancia.task; i++)
                {
                    for (int j = 0; j < instancia.date; j++)
                    {
                        exp.Clear();
                        for (int n = 0; n < instancia.crew; n++)
                        {
                            exp.AddTerm(1, z[n, i, j]);
                        }
                        modelo.AddConstr(exp <= instancia.d2[i] * x2[i, j], "R15_" + i + "_" + j);
                    }
                }
                //restrição 16
                //número mínimo de membros para trabalhar em um tarefa deve ser respeitado
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int i = 0; i < instancia.task; i++)
                    {
                        for (int j = 0; j < instancia.date; j++)
                        {
                            modelo.AddConstr(x[n, i, j] <= x3[i, j] / instancia.d1[i], "R16_" + n + "_" + i + "_" + j);
                        }
                    }
                }
                //restrição 17
                //membros de equipe não podem trabalhar em dias em que eles não estão disponíveis
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int i = 0; i < instancia.task; i++)
                    {
                        for (int j = 0; j < instancia.date; j++)
                        {
                            modelo.AddConstr(z[n, i, j] <= instancia.e[n, j], "R17_" + n + "_" + i + "_" + j);
                        }
                    }
                }

                //Restrições com relação a competência

                //restrição 18
                //a combinação do nível de competencias de todos os membros 
                //de equipe deve ser suficiente para cada tarefa
                for (int i = 0; i < instancia.task; i++)
                {
                    for (int j = 0; j < instancia.date; j++)
                    {
                        for (int k = 0; k < instancia.competencies; k++)
                        {
                            exp.Clear();
                            for (int n = 0; n < instancia.crew; n++)
                            {
                                exp.AddTerm(instancia.bm3[n, k], z[n, i, j]);
                            }
                            modelo.AddConstr(exp >= x2[i, j] * instancia.bo[i, k] * instancia.level_total, "R18_" + i + "_" + j + "_" + k);
                        }
                    }
                }
                //restrição 19
                //pelo menos um membro de equipe deve ter nível 3 de competencia para a tarefa i
                for (int i = 0; i < instancia.task; i++)
                {
                    for (int j = 0; j < instancia.date; j++)
                    {
                        for (int k = 0; k < instancia.competencies; k++)
                        {
                            exp.Clear();
                            for (int n = 0; n < instancia.crew; n++)
                            {
                                exp.AddTerm(instancia.bm[n, k], z[n, i, j]);
                            }
                            modelo.AddConstr(exp >= x2[i, j] * instancia.bo[i, k], "R19_" + i + "_" + j + "_" + k);
                        }
                    }
                }
                //restrição 20
                //pelo menos um mebro de equipe tem nível de competencia 3 se vários membros de equipe trabalham na mesma tarefa
                for (int i = 0; i < instancia.task; i++)
                {
                    for (int j = 0; j < instancia.date; j++)
                    {
                        for (int k = 0; k < instancia.competencies; k++)
                        {
                            exp.Clear();
                            exp2.Clear();
                            for (int n = 0; n < instancia.crew; n++)
                            {
                                exp.AddTerm(instancia.bm[n, k], x[n, i, j]);
                                exp2.AddTerm(instancia.bm2[n, k], x[n, i, j]);
                            }
                            modelo.AddConstr(exp >= exp2 * (double)(1 / instancia.d1[i]), "R20_" + i + "_" + j + "_" + k);
                        }
                    }
                }

                //Restrições com relação ao transporte

                //restrição 21
                //cada membro de equipe trabalha em um local técnico em que a tarefa está localizada
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int p = 0; p < instancia.technical_place; p++)
                    {
                        for (int j = 0; j < instancia.date; j++)
                        {
                            exp.Clear();
                            for (int i = 0; i < instancia.task; i++)
                            {
                                exp.AddTerm(instancia.tp[i, p], z[n, i, j]);
                            }
                            modelo.AddConstr(w[n, p, j] <= exp, "R21_" + n + "_" + p + "_" + j);
                        }
                    }
                }
                //restrição 22
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int p = 0; p < instancia.technical_place; p++)
                    {
                        for (int j = 0; j < instancia.date; j++)
                        {
                            exp.Clear();
                            for (int i = 0; i < instancia.task; i++)
                            {
                                exp.AddTerm(instancia.tp[i, p], z[n, i, j]);
                            }
                            modelo.AddConstr(w[n, p, j] * M >= exp, "R22_" + n + "_" + p + "_" + j);
                        }
                    }
                }
                //restrição 23
                //o membro de equipe só é transportado entre os locais técnicos que as tarefas dele estão localizadas
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int p = 0; p < instancia.technical_place; p++)
                    {
                        for (int j = 0; j < instancia.date; j++)
                        {
                            exp.Clear();
                            for (int q = 0; q < instancia.technical_place; q++)
                            {
                                exp.AddTerm(1, v[n, p, q, j]);
                            }
                            modelo.AddConstr(exp <= w[n, p, j] * M, "R23_" + n + "_" + p + "_" + j);
                        }
                    }
                }
                //restrição 24
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int q = 0; q < instancia.technical_place; q++)
                    {
                        for (int j = 0; j < instancia.date; j++)
                        {
                            exp.Clear();
                            for (int p = 0; p < instancia.technical_place; p++)
                            {
                                exp.AddTerm(1, v[n, p, q, j]);
                            }
                            modelo.AddConstr(exp <= w[n, q, j] * M, "R24_" + n + "_" + q + "_" + j);
                        }
                    }
                }
                //restrição 25
                //se o membro de equipe trabalha em mais do que um local técninco durante o turno
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int q = 0; q < instancia.technical_place; q++)
                    {
                        for (int j = 0; j < instancia.date; j++)
                        {
                            exp.Clear();
                            for (int p = 0; p < instancia.technical_place; p++)
                            {
                                exp.AddTerm(1, v[n, p, q, j]);
                            }
                            modelo.AddConstr(w1[n, q, j] == exp, "R25_" + n + "_" + q + "_" + j);
                        }
                    }
                }
                //restrição 26
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int p = 0; p < instancia.technical_place; p++)
                    {
                        for (int j = 0; j < instancia.date; j++)
                        {
                            exp.Clear();
                            for (int q = 0; q < instancia.technical_place; q++)
                            {
                                exp.AddTerm(1, v[n, p, q, j]);
                            }
                            modelo.AddConstr(w2[n, p, j] == exp, "R26_" + n + "_" + p + "_" + j);
                        }
                    }
                }
                //restrição 27
                //cada membro de equipe pode apenas ser transportado de e para cada local técnico uma vez por dia
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int p = 0; p < instancia.technical_place; p++)
                    {
                        for (int j = 0; j < instancia.date; j++)
                        {
                            modelo.AddConstr(w1[n, p, j] <= 1, "R27_" + n + "_" + p + "_" + j);
                        }
                    }
                }

                //restrição 28
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int p = 0; p < instancia.technical_place; p++)
                    {
                        for (int j = 0; j < instancia.date; j++)
                        {
                            modelo.AddConstr(w2[n, p, j] <= 1, "R28_" + n + "_" + p + "_" + j);
                        }
                    }
                }
                //restrição 29
                //funcionário será transportado apenas uma vez do e para o depósito 
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int j = 0; j < instancia.date; j++)
                    {
                        exp.Clear();
                        for (int p = 0; p < instancia.technical_place; p++)
                        {
                            exp.AddTerm(2, w[n, p, j]);
                            exp.AddTerm(-1, w1[n, p, j]);
                            exp.AddTerm(-1, w2[n, p, j]);
                        }
                        modelo.AddConstr(exp == 2 * y[n, j], "R29_" + n + "_" + j);
                    }
                }

                //aleatório
                int semente = 0;
                Random aleatorio = new Random(semente);
                //relógio
                Stopwatch relogio = new Stopwatch();

                //variável para contar quantidade de soluções sem melhoria
                int sem_melhoria = 0;
                //variável para guardar melhor scenário
                int melhorCenario = 0;
                //variável para guardar número de cenários
                int num_scenarios = 5;

                //função que gera números aleatórios sem repetir
                int[] AleatorioaSemRepetir(int quantidade, int minimo, int maximo)
                {
                    int sorteado;
                    int[] NoRepet = new int[quantidade];
                    int posicao = 0;
                    List<int> guarda = new List<int>();

                    while (posicao < quantidade)
                    {
                        sorteado = aleatorio.Next(minimo, maximo);
                        if (!guarda.Contains(sorteado))
                        {
                            guarda.Add(sorteado);
                            NoRepet[posicao] = sorteado;
                            posicao++;
                        }
                    }
                    return NoRepet;
                }

                //pega a primeira solução factível do gurobi
                relogio.Start();
                modelo.Parameters.SolutionLimit = 1;
                modelo.Optimize();

                //matriz para guardar melhor z 
                double[,,] melhor_z = new double[instancia.crew, instancia.task, instancia.date];
                for (int n = 0; n < instancia.crew; n++)
                {
                    for (int i = 0; i < instancia.task; i++)
                    {
                        for (int j = 0; j < instancia.date; j++)
                        {
                            melhor_z[n, i, j] = z[n, i, j].X;
                        }
                    }
                }
                //melhor FO
                double bestFO = modelo.ObjVal;

                //mudar o gurobi para voltar a resolver o problema por completo
                modelo.Parameters.SolutionLimit = 2000000000;
                modelo.Set(GRB.DoubleParam.TimeLimit, 7200);

                //função fix and optimize para datas
                void multiCenariosFixAndOptimizeData(int num_scenarios)
                {               
                    // variável para guardar número de cenários
                    int scenarios=0;
                    //seta o parâmetro do modelo para multiplos scenários
                    modelo.NumScenarios = num_scenarios;
                    //quantas datas ficarão fixas
                    int datasFixas = instancia.date - 2;
                    //vetros que irá guardar datas sorteadas para serem fixas
                    int[] vetorDatas = new int[datasFixas];
                    //criar os cenários a serem otimizados pelo Gurobi
                    while (scenarios < num_scenarios)
                    {
                        modelo.Parameters.ScenarioNumber = scenarios;
                        //sorteia datas que serão fixadas
                        vetorDatas = AleatorioaSemRepetir(datasFixas, 0, instancia.date);
                        for (int n = 0; n < instancia.crew; n++)
                        {
                            for (int i = 0; i < instancia.task; i++)
                            {
                                for (int j = 0; j < instancia.date; j++)
                                {
                                    if (vetorDatas.Contains(j))
                                    {
                                        z[n, i, j].ScenNUB = melhor_z[n, i, j];
                                        z[n, i, j].ScenNLB = melhor_z[n, i, j];
                                    }
                                    else
                                    {
                                        z[n, i, j].Start = melhor_z[n, i, j];
                                    }
                                }
                            }
                        }
                        semente++;
                        scenarios++;
                    }
                    // Solve multi-scenario model
                    modelo.Optimize();               
                    // descobrir qual melhor cenário
                    for(int s= 0;s<num_scenarios;s++)
                    {
                        modelo.Parameters.ScenarioNumber = s;
                        // o comando modelo.ScenNObjVal devolve o valor da função objetivo do cenário atual
                        double atual_FO = modelo.ScenNObjVal;
                        if (atual_FO < bestFO)
                        {
                            //atualiza melhor cenário e melhor FO
                            bestFO = atual_FO;
                            melhorCenario = s;                                             
                        }
                        else
                        {
                            sem_melhoria++;
                        }
                    }
                    //atualiza a melhor solução de z
                    modelo.Parameters.ScenarioNumber = melhorCenario;
                    for(int n = 0; n < instancia.crew; n++)
                    {
                        for(int i = 0; i < instancia.task; i++)
                        {
                            for(int j = 0; j < instancia.date; j++)
                            {
                                melhor_z[n, i, j] = z[n, i, j].ScenNX;
                            }
                        }
                    }
                }

                // Fix and Optimize Datas   

                //int num_scenarios = 5;
                //while (sem_melhoria < 20)
                //{
                //    multiCenariosFixAndOptimizeData(num_scenarios);
                //    //escreve no console os cenários e suas respectivas FOs               
                //    for (int i = 0; i < num_scenarios; i++)
                //    {
                //        modelo.Parameters.ScenarioNumber = i;
                //        Console.WriteLine("Solucao para cenario" + i.ToString() + ":" + modelo.ScenNObjVal.ToString());
                //    }

                //}
                //relogio.Stop();
                ////a variável novoModelo recebe o modelo com melhor cenário
                //modelo.Parameters.ScenarioNumber = melhorCenario;
                //System.Console.WriteLine("Melhor FO:" + modelo.ObjVal.ToString());
                //System.Console.WriteLine($"Tempo total {relogio.ElapsedMilliseconds / 1000} segundos");

                //Função Fix and Optimize equipes
                void multiCenariosFixAndOptimizeEquipes(int num_scenarios)
                {

                    // variável para guardar número de cenários
                    int scenarios = 0;
                    //seta o parâmetro do modelo para multiplos scenários
                    modelo.NumScenarios = num_scenarios;
                    //quantas datas ficarão fixas
                    int equipesFixas = instancia.crew - 2;
                    //vetros que irá guardar datas sorteadas para serem fixas
                    int[] vetorEquipes = new int[equipesFixas];
                    //criar os cenários a serem otimizados pelo Gurobi
                    while (scenarios < num_scenarios)
                    {
                        modelo.Parameters.ScenarioNumber = scenarios;
                        //sorteia datas que serão fixadas
                        vetorEquipes = AleatorioaSemRepetir(equipesFixas, 0, instancia.crew);
                        foreach(int n in vetorEquipes)
                        {
                            for(int i = 0; i < instancia.task; i++)
                            {
                                for (int j = 0; j < instancia.date; j++)
                                {
                                    z[n, i, j].ScenNUB = melhor_z[n, i, j];
                                    z[n, i, j].ScenNLB = melhor_z[n, i, j];
                                }
                            }
                        }                  
                        semente++;
                        scenarios++;
                    }
                    // Solve multi-scenario model
                    modelo.Optimize();
                    // descobrir qual melhor cenário
                    for (int s = 0; s < num_scenarios; s++)
                    {
                        modelo.Parameters.ScenarioNumber = s;
                        // o comando modelo.ScenNObjVal devolve o valor da função objetivo do cenário atual
                        double atual_FO = modelo.ScenNObjVal;
                        if (atual_FO < bestFO)
                        {
                            //atualiza melhor cenário e melhor FO
                            bestFO = atual_FO;
                            melhorCenario = s;                        
                        }
                        else
                        {
                            sem_melhoria++;
                        }
                    }
                    modelo.Parameters.ScenarioNumber = melhorCenario;
                    //atualiza a melhor solução de z
                    for (int n = 0; n < instancia.crew; n++)
                    {
                        for (int i = 0; i < instancia.task; i++)
                        {
                            for (int j = 0; j < instancia.date; j++)
                            {
                                melhor_z[n, i, j] = z[n, i, j].ScenNX;
                            }
                        }
                    }
                }

                // Fix and Optimize equipes

                //int num_scenarios = 5;
                //while (sem_melhoria < 20)
                //{
                //    multiCenariosFixAndOptimizeEquipes(num_scenarios);
                //    //escreve no console os cenários e suas respectivas FOs               
                //    for (int i = 0; i < num_scenarios; i++)
                //    {
                //        modelo.Parameters.ScenarioNumber = i;
                //        Console.WriteLine("Solucao para cenario" + i.ToString() + ":" + modelo.ScenNObjVal.ToString());
                //    }

                //}
                //relogio.Stop();
                ////a variável novoModelo recebe o modelo com melhor cenário
                //modelo.Parameters.ScenarioNumber = melhorCenario;
                //System.Console.WriteLine("Melhor FO:" + modelo.ObjVal.ToString());
                //System.Console.WriteLine($"Tempo total {relogio.ElapsedMilliseconds / 1000} segundos");

                // Função Fix and Optimize Tarefas

                void multiCenariosFixAndOptimizeTarefas(int num_scenarios)
                {

                    // variável para guardar número de cenários
                    int scenarios = 0;
                    //seta o parâmetro do modelo para multiplos scenários
                    modelo.NumScenarios = num_scenarios;
                    //quantas datas ficarão fixas
                    int tarefasFixas = instancia.task - 2;
                    //vetros que irá guardar datas sorteadas para serem fixas
                    int[] vetorTarefas = new int[tarefasFixas];
                    
                    //criar os cenários a serem otimizados pelo Gurobi
                    while (scenarios < num_scenarios)
                    {
                        modelo.Parameters.ScenarioNumber = scenarios;
                        //sorteia datas que serão fixadas
                        vetorTarefas = AleatorioaSemRepetir(tarefasFixas, 0, instancia.task);
                        for(int n=0;n<instancia.crew;n++)
                        {
                            foreach(int i in vetorTarefas)
                            {
                                for (int j = 0; j < instancia.date; j++)
                                {
                                    z[n, i, j].ScenNUB = melhor_z[n, i, j];
                                    z[n, i, j].ScenNLB = melhor_z[n, i, j];
                                }
                            }
                        }
                        semente++;
                        scenarios++;
                    }
                    // Solve multi-scenario model
                    modelo.Optimize();              
                }

                // Fix and Optimize tarefas


                while (sem_melhoria < 50)
                {
                    //desfixar variáveis z
                    for (int n = 0; n < instancia.crew; n++)
                    {
                        for (int i = 0; i < instancia.task; i++)
                        {
                            for (int j = 0; j < instancia.date; j++)
                            {
                                z[n, i, j].LB = 0.0;
                                z[n, i, j].UB = 1.0;
                            }
                        }
                    }
                    multiCenariosFixAndOptimizeTarefas(num_scenarios);
                    for (int s = 0; s < num_scenarios; s++)
                    {
                        modelo.Parameters.ScenarioNumber = s;
                        double atualFO = modelo.ScenNObjVal;
                        if (atualFO < bestFO)
                        {
                            bestFO = atualFO;
                            melhorCenario = s;
                            for (int n = 0; n < instancia.crew; n++)
                            {
                                for (int i = 0; i < instancia.task; i++)
                                {
                                    for (int j = 0; j < instancia.date; j++)
                                    {
                                        melhor_z[n, i, j] = z[n, i, j].ScenNX;
                                    }
                                }
                            }
                        }
                    }
                    //escreve no console os cenários e suas respectivas FOs
                    for (int i = 0; i < num_scenarios; i++)
                    {
                        modelo.Parameters.ScenarioNumber = i;
                        Console.WriteLine("Solucao para cenario " + i.ToString() + ":" + modelo.ScenNObjVal.ToString());
                    }
                }
                relogio.Stop();
                //a variável novoModelo recebe o modelo com melhor cenário
                modelo.Parameters.ScenarioNumber = melhorCenario;
                System.Console.WriteLine("Melhor FO:" + modelo.ScenNObjVal.ToString());
                System.Console.WriteLine("Melhor FO:" + bestFO.ToString());
                System.Console.WriteLine($"Tempo total {relogio.ElapsedMilliseconds / 1000} segundos");

                // Seta classe Resultados
                Resultado resultado = new Resultado();
                resultado.nome = arquivo;
                resultado.segundos = relogio.ElapsedMilliseconds / 1000;
                resultado.valor = modelo.ObjVal;
                string output = JsonConvert.SerializeObject(resultado);
                System.IO.File.AppendAllText("final.txt", output);
            }
        }
    }
}