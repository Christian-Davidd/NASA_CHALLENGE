using System;

namespace MarteSim.ClimaEspacial
{
    [Serializable]
    public class ConfiguracionTiempoJuego
    {
        public float duracion_max_partida_seg;
        public int total_dias_juego;
        public float segundos_por_dia;
    }

    [Serializable]
    public class DistribucionSeveridad
    {
        public float C;
        public float M;
        public float X;
    }

    [Serializable]
    public class EstadisticasClimaNasa
    {
        public string ultima_actualizacion_utc;
        public int total_llamaradas_semana;
        public float probabilidad_evento_por_dia;
        public float duracion_media_tormenta_seg;
        public ConfiguracionTiempoJuego configuracion_tiempo_juego;
        public DistribucionSeveridad distribucion_severidad;
    }

    [Serializable]
    public struct DatosTormentaActiva
    {
        public int dia;
        public string claseSolar;           // "C", "M" o "X"
        public string nivelAlerta;          // "ALERTA MENOR", "MODERADA" o "CRÍTICA"
        public float duracionTotalSeg;
        public float drenajeBateriaPorSeg;  // % de batería/seg que exige el escudo
        public float danoRadiacionPorSeg;   // % de daño/seg sin escudo ni refugio
    }
}