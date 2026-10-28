/*
   Data última atualização: 28/10/2026
   Estudante: Maria Eduarda Bambini
   Arquivo auxiliar para estilização do terminal.
*/

package common;

import java.time.LocalTime;
import java.time.format.DateTimeFormatter;

/** Cores ANSI e funções de apoio para a saída estilizada no terminal. */
public final class Style {

    public static final String RESET = "\033[0m";
    public static final String BOLD = "\033[1m";
    public static final String RED = "\033[38;5;203m";
    public static final String GREEN = "\033[38;5;114m";
    public static final String YELLOW = "\033[38;5;221m";
    public static final String CYAN = "\033[38;5;80m";
    public static final String BLUE = "\033[38;5;75m";
    public static final String MAGENTA = "\033[38;5;177m";
    public static final String GRAY = "\033[38;5;245m";

    public static volatile String currentPrompt = "";

    private static final DateTimeFormatter CLOCK = DateTimeFormatter.ofPattern("HH:mm:ss");

    private Style() {
    }

    public static synchronized void printLine(String line) {
        System.out.print("\r\033[2K");
        System.out.println(line);
        System.out.print(currentPrompt);
        System.out.flush();
    }

    private static void log(String color, String level, String text) {
        String time = LocalTime.now().format(CLOCK);
        printLine(GRAY + "[" + time + " " + color + BOLD + level + RESET + GRAY + "]" + RESET + " " + text);
    }

    public static void showInfo(String text) {
        log(CYAN, "INF", BLUE + text + RESET);
    }

    public static void showSuccess(String text) {
        log(GREEN, "SUC", GREEN + text + RESET);
    }

    public static void showWarning(String text) {
        log(YELLOW, "WRN", YELLOW + text + RESET);
    }

    public static void showError(String text) {
        log(RED, "ERR", RED + text + RESET);
    }

    /** Monta um prompt colorido; o texto digitado pelo usuário continua na cor padrão. */
    public static String prompt(String label) {
        return BLUE + BOLD + label + RESET;
    }

    /** Imprime um campo no formato "Rótulo:     valor", com os valores alinhados. */
    public static void field(String label, String value) {
        String text = value.isEmpty() ? "—" : value;
        printLine(GRAY + String.format("%-15s", label + ":") + RESET + text);
    }
}