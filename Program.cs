using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Gurobi;
using Newtonsoft.Json;

namespace RelaxAndFix_cs
{
  class Program
  {
    static void Main(string[] args)
    {
      //Deserializar o arquivo Json para o c#
      var json = File.ReadAllText("instanciaT11E8D10F2.json");

      //   object serializer = JsonConvert.DeserializeObject(json);

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
      //int semente = 3;
      Random aleatorio = new Random();
      Stopwatch relogio = new Stopwatch();

      relogio.Start();

      //pega a primeira solução factível do gurobi
      modelo.Parameters.SolutionLimit = 1;

      modelo.Optimize();

      // variável para guardar número de cenários
      int scenarios;
      int num_scenarios;

      // guarda a solução inicial
      double origObjVal = modelo.ObjVal;
      GRBVar[] vars = modelo.GetVars();
      GRBVar[] varsZ = modelo.GetVars().Where(h => h.VarName.Substring(0, 1) == "z").ToArray();
      double[] origX = modelo.Get(GRB.DoubleAttr.X, vars);
      double[] origZ = modelo.Get(GRB.DoubleAttr.X, varsZ);

      num_scenarios = 5;

      //mudar o gurobi para voltar a resolver o problema por completo
      modelo.Parameters.SolutionLimit = 2000000000;

      // setar o número de senários que terá o modelo
      modelo.NumScenarios = num_scenarios;

      scenarios = 0;


      //Relax and Fix para tarefas

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

      //deixar livre apenas duas tarefas
      int tarefasFixas = instancia.task - 2;
      //sortear tarefas que serão fixadas
      int[] vetorTarefas = new int[tarefasFixas];

      while (scenarios < num_scenarios)
      {
        modelo.Parameters.ScenarioNumber = scenarios;
        vetorTarefas = AleatorioaSemRepetir(tarefasFixas, 0, instancia.task);
        for (int i = 0; i < varsZ.Length; i++)
        {
          GRBVar variavel = varsZ[i];
          string[] indice = new string[4];
          indice = variavel.VarName.Split("_");

          if (vetorTarefas.Contains(int.Parse(indice[2])))
          {
            if (origZ[i] < 0.5)
            {
              variavel.ScenNUB = 0.0;
              variavel.ScenNLB = 0.0;
            }
            else
            {
              variavel.ScenNUB = 1.0;
              variavel.ScenNLB = 1.0;
            }
          }
          else
          {
            variavel.Start = origZ[i];
          }
        }
        scenarios++;
      }
      // Solve multi-scenario model
      modelo.Optimize();

      for (int i = 0; i < num_scenarios; i++)
      {
        modelo.Parameters.ScenarioNumber = i;
        Console.WriteLine("Solucao para cenario" + i.ToString() + ":" + modelo.ScenNObjVal.ToString());
      }

      System.Console.WriteLine($"Tempo total {relogio.ElapsedMilliseconds / 1000} segundos");

    }
  }
}